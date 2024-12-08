using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Data.Eval;
using GoLive.Generator.RazorPageRoute.Generator.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;


namespace GoLive.Generator.RazorPageRoute.Generator;

[Generator]
public class PageRouteIncrementalExperimentalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var defaultNamespace = context.AnalyzerConfigOptionsProvider.Select((provider, _) => !provider.GlobalOptions.TryGetValue("build_property.rootnamespace", out var ns) ? "DefaultNamespace" : ns);

        var projectDirProvider = context.AnalyzerConfigOptionsProvider.Select((provider, _) =>
        {
            /*var globalOptions = new Dictionary<string, string>();
            foreach (var option in provider.GlobalOptions.Keys)
            {
                if (provider.GlobalOptions.TryGetValue(option, out var value))
                {
                    globalOptions[option] = value;
                }
            }*/

            if (!provider.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir))
            {
                return null;
            }
            return projectDir;
        });
        
        var configFiles = context.AdditionalTextsProvider.Where(IsConfigurationFile);

        var pageRouteItems = projectDirProvider.Combine(configFiles.Collect()).Select((projectPathAndConfigFiles, _) =>
        {
            var (projectPath, configFiles) = projectPathAndConfigFiles;

            if (projectPath == null)
            {
                return default;
            }

            var config = LoadConfig(configFiles, "DefaultNamespace");

            var dllFile = Scanner.GetDllPathFromProject(projectPath, out var assemblyResolver);

            using var assembly = AssemblyDefinition.ReadAssembly(dllFile, new ReaderParameters { AssemblyResolver = assemblyResolver });

            var routes = Scanner.ScanForPageRoutesIncremental(assembly, config).CustomDistinctBy(e => e.Route).ToList();
            var incrementals = Scanner.ScanForInvokables(assembly).ToList();

            return (routes, incrementals);
        });

        context.RegisterSourceOutput(pageRouteItems.Combine(configFiles.Collect()).Combine(defaultNamespace), Output);
    }

    private void Output(SourceProductionContext productionContext, (((List<PageRoute> routes, List<(string MethodName, string InvokableName)> incrementals) Left, ImmutableArray<AdditionalText> Right) Left, string defaultNamespace) arg2)
    {
        var config = LoadConfig(arg2.Left.Right, arg2.defaultNamespace);
        
        GenerateOutput(productionContext, config, arg2.Left.Left.routes);
        
        if (config.Invokables is { Enabled: true })
        {
            GenerateJSInvokable(config, arg2.Left.Left.incrementals);
        }
    }

    public static void GenerateOutput(SourceProductionContext productionContext, Settings config, List<PageRoute> pageRoutes)
    {
        var source = new SourceStringBuilder();

        if (config.OutputLastCreatedTime)
        {
            source.AppendLine($"// This file was generated on {DateTime.Now:R}");
        }

        source.AppendLine("using System;");
        source.AppendLine("using System.Net.Http;");
        source.AppendLine("using System.Threading.Tasks;");
        source.AppendLine("using System.Net.Http.Json;");
        source.AppendLine("using System.Collections.Generic;");

        if (config.OutputIAuthorizeData)
        {
            source.AppendLine("using Microsoft.AspNetCore.Authorization;");
            source.AppendLine("using Microsoft.AspNetCore.Components;");
        }

        if (config.OutputExtensionMethod)
        {
            source.AppendLine("using Microsoft.AspNetCore.Components;");
        }

        source.AppendLine($"namespace {config.Namespace}");
        source.AppendOpenCurlyBracketLine();
        source.AppendLine($"public static partial class {config.ClassName}");
        source.AppendOpenCurlyBracketLine();

        if (pageRoutes.Count == 0)
        {
            return;
        }

        foreach (var pageRoute in pageRoutes)
        {
            var routeTemplate = TemplateParser.ParseTemplate(pageRoute.Route);

            var SlugName = Slug.Create(pageRoute.Route.Length > 1 ? string.Join(".", routeTemplate.Segments.Where(f => !f.IsParameter).Select(f => f.Value)) : "Home");

            if (string.IsNullOrWhiteSpace(SlugName))
            {
                SlugName = pageRoute.Name;
            }

            var routeSegments = routeTemplate.Segments.Where(e => e.IsParameter).Select(delegate(TemplateSegment segment)
            {
                var constraint = segment.Constraints.Any() ? segment.Constraints.FirstOrDefault().GetConstraintType() : null;

                if (constraint == null)
                {
                    return $"string {segment.Value}";
                }

                return segment.IsOptional ? $"{constraint.FullName}? {segment.Value}" : $"{constraint.FullName} {segment.Value}";
            }).ToList();

            if (pageRoute.QueryString is { Count: > 0 })
            {
                routeSegments.AddRange(pageRoute.QueryString.Select(prqp => $"{prqp.Type} {prqp.Name} = default"));
            }

            var parameterString = string.Join(", ", routeSegments);

            OutputRouteStringMethod(source, SlugName, parameterString, routeTemplate, pageRoute);

            if (config.OutputExtensionMethod)
            {
                OutputRouteExtensionMethod(source, SlugName, parameterString, routeTemplate, pageRoute, config);
            }
        }

        source.AppendCloseCurlyBracketLine();
        source.AppendCloseCurlyBracketLine();

        var sourceOutput = source.ToString();

        if (!string.IsNullOrWhiteSpace(sourceOutput))
        {
            if (config.OutputToFiles.Count > 0)
            {
                foreach (var configOutputToFile in config.OutputToFiles)
                {
                    File.WriteAllText(configOutputToFile, sourceOutput);
                }
            }
        }
    }

    private static void OutputRouteStringMethod(SourceStringBuilder source, string SlugName, string parameterString, RouteTemplate routeTemplate, PageRoute pageRoute)
    {
        source.AppendLine($"public static string {SlugName} ({parameterString})");
        source.AppendOpenCurlyBracketLine();

        if (routeTemplate.Segments.Any(e => e.IsParameter))
        {
            source.AppendIndent();
            source.Append("string url = $\"", false);

            foreach (var seg in routeTemplate.Segments)
            {
                if (seg.IsParameter)
                {
                    source.Append($"/{{{seg.Value}.ToString()}}", false);
                }
                else
                {
                    source.Append($"/{seg.Value}", false);
                }
            }

            source.Append("\";\n", false);
        }
        else
        {
            source.AppendLine($"string url = \"{pageRoute.Route}\";");
        }

        if (pageRoute.QueryString is { Count: > 0 })
        {
            source.AppendLine("Dictionary<string, string> queryString=new();");

            foreach (var pageRouteQuerystringParameter in pageRoute.QueryString)
            {
                if (pageRouteQuerystringParameter.Type == "System.String" || pageRouteQuerystringParameter.Type.ToLower() == "string")
                {
                    source.AppendLine($"if (!string.IsNullOrWhiteSpace({pageRouteQuerystringParameter.Name})) ");
                }
                else
                {
                    source.AppendLine($"if ({pageRouteQuerystringParameter.Name} != default) ");
                }

                source.AppendOpenCurlyBracketLine();
                source.AppendLine($"queryString.Add(\"{pageRouteQuerystringParameter.Name}\", {pageRouteQuerystringParameter.Name}.ToString());");
                source.AppendCloseCurlyBracketLine();
            }

            source.AppendLine("");
            source.AppendLine("url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);");
        }

        source.AppendLine("return url;");

        source.AppendCloseCurlyBracketLine();
    }


    private static void OutputRouteExtensionMethod(SourceStringBuilder source, string SlugName, string parameterString, RouteTemplate routeTemplate, PageRoute pageRoute, Settings config)
    {
        if (pageRoute.Auth is { RequiresAuthentication: true })
        {
            if (config.OutputIAuthorizeData)
            {
                source.AppendLine($"public class {SlugName}_AuthData : IAuthorizeData");
                source.AppendOpenCurlyBracketLine();

                if (pageRoute.Auth.CustomAuth is { Count: > 0 })
                {
                    var authItem = pageRoute.Auth.CustomAuth.FirstOrDefault();
                    var authSettings = config.Auth.First(e => e.Attribute == authItem.Name);

                    var policyList = EvaluateCode(authSettings.PolicyTransformer, authItem);
                    var rolesList = EvaluateCode(authSettings.RolesTransformer, authItem);
                    var authSchemesList = EvaluateCode(authSettings.AuthenticationSchemeTransformer, authItem);

                    source.AppendLine($"public string Policy {{ get; set; }} = {(string.Join(",", policyList ?? []).Length == 0 ? "String.Empty" : $"\"{string.Join(", ", policyList)}\"")}; ");
                    source.AppendLine($"public string Roles {{ get; set; }} = {(string.Join(",", rolesList ?? []).Length == 0 ? "String.Empty" : $"\"{string.Join(", ", rolesList)}\"")}; ");
                    source.AppendLine($"public string AuthenticationSchemes {{ get; set; }} = {(string.Join(",", authSchemesList ?? []).Length == 0 ? "String.Empty" : $"\"{string.Join(", ", authSchemesList)}\"")}; ");
                }
                else
                {
                    source.AppendLine($"public string Policy {{ get; set; }} = {(string.Join(",", pageRoute.Auth.Policies ?? []).Length == 0 ? "String.Empty" : $"\"{string.Join(",", pageRoute.Auth.Policies)}\"")};");
                    source.AppendLine($"public string Roles {{ get; set; }} = {(string.Join(",", pageRoute.Auth.Roles ?? []).Length == 0 ? "String.Empty" : $"\"{string.Join(",", pageRoute.Auth.Roles)}\"")}; ");
                    source.AppendLine($"public string AuthenticationSchemes {{ get; set; }} = {(string.Join(",", pageRoute.Auth.AuthenticationSchemes ?? []).Length == 0 ? "String.Empty" : $"\"{string.Join(", ", pageRoute.Auth.AuthenticationSchemes)}\"")}; ");
                }

                source.AppendCloseCurlyBracketLine();
            }


            source.AppendLine("/// <summary>");
            source.AppendLine($"/// Page Requires Authentication{(pageRoute.Auth.CustomAuth != null ? ", Custom Authentication Provider (CustomAuth)" : "")}");

            if (pageRoute.Auth.Roles != null && pageRoute.Auth.Roles.Any())
            {
                source.AppendLine($"/// Roles: {string.Join(", ", pageRoute.Auth.Roles)}");
            }

            if (pageRoute.Auth.Policies != null && pageRoute.Auth.Policies.Any())
            {
                source.AppendLine($"/// Policies: {string.Join(", ", pageRoute.Auth.Policies)}");
            }

            if (pageRoute.Auth.CustomAuth != null)
            {
                foreach (var customAuth in pageRoute.Auth.CustomAuth)
                {
                    source.AppendLine($"/// Custom Authentication ProviderName: {customAuth.Name}");
                    source.AppendLine($"/// Custom Auth Constructor Params: {string.Join(", ", customAuth.CtorParams)}");
                    source.AppendLine($"/// Custom Auth Named Params: {string.Join(", ", customAuth.NamedParams)}");
                }
            }

            source.AppendLine("/// </summary>");
        }

        if (string.IsNullOrWhiteSpace(parameterString))
        {
            source.AppendLine($"public static void {SlugName} (this NavigationManager manager, bool forceLoad = false, bool replace=false)");
        }
        else
        {
            source.AppendLine($"public static void {SlugName} (this NavigationManager manager, {parameterString}, bool forceLoad = false, bool replace=false)");
        }

        source.AppendOpenCurlyBracketLine();

        if (routeTemplate.Segments.Any(e => e.IsParameter))
        {
            source.AppendIndent();
            source.Append("string url = $\"", false);

            foreach (var seg in routeTemplate.Segments)
            {
                if (seg.IsParameter)
                {
                    source.Append($"/{{{seg.Value}.ToString()}}", false);
                }
                else
                {
                    source.Append($"/{seg.Value}", false);
                }
            }

            source.Append("\";\n", false);
        }
        else
        {
            source.AppendLine($"string url = \"{pageRoute.Route}\";");
        }

        if (pageRoute.QueryString is { Count: > 0 })
        {
            source.AppendLine("Dictionary<string, string> queryString=new();");

            foreach (var pageRouteQuerystringParameter in pageRoute.QueryString)
            {
                if (pageRouteQuerystringParameter.Type == "System.String" || pageRouteQuerystringParameter.Type.ToLower() == "string")
                {
                    source.AppendLine($"if (!string.IsNullOrWhiteSpace({pageRouteQuerystringParameter.Name})) ");
                }
                else
                {
                    source.AppendLine($"if ({pageRouteQuerystringParameter.Name} != default) ");
                }

                source.AppendOpenCurlyBracketLine();
                source.AppendLine($"queryString.Add(\"{pageRouteQuerystringParameter.Name}\", {pageRouteQuerystringParameter.Name}.ToString());");
                source.AppendCloseCurlyBracketLine();
            }

            source.AppendLine("");
            source.AppendLine("url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);");
        }

        source.AppendLine("manager.NavigateTo(url, forceLoad, replace);");
        source.AppendCloseCurlyBracketLine();
    }

    private static List<string> EvaluateCode(string transformer, PageRouteAuthCustomAuth authItem)
    {
        if (string.IsNullOrWhiteSpace(transformer))
        {
            return null;
        }
        Evaluator policyEvaluator = new(transformer);
        policyEvaluator.AddUsing("System.Collections.Generic");
        policyEvaluator["ConstructorParameters"] = authItem.CtorParams;
        policyEvaluator["NamedParameters"] = authItem.NamedParams;
        var policyList = policyEvaluator.Eval<List<string>>();
        return policyList;
    }


    public static Settings LoadConfig(IEnumerable<AdditionalText> configFiles, string defaultNamespace)
    {
        var configFilePath = configFiles.FirstOrDefault();

        if (configFilePath == null)
        {
            return null;
        }

        var filePath = configFilePath.Path;
        return LoadConfigFromFile(filePath, defaultNamespace);
    }

    public static Settings LoadConfigFromFile(string filePath, string defaultNamespace)
    {
        var jsonString = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<Settings>(jsonString);
        var configFileDirectory = Path.GetDirectoryName(filePath);

        if (string.IsNullOrEmpty(config.Namespace))
        {
            config.Namespace = defaultNamespace;
        }

        if (config.OutputToFiles != null && config.OutputToFiles.Any())
        {
            config.OutputToFiles = config.OutputToFiles.Select(r =>
            {
                var fullPath = Path.Combine(configFileDirectory, r);

                return fullPath;
            }).ToList();
        }
        
        if (config.Invokables != null && config.Invokables.OutputToFiles.Count > 0)
        {
            foreach (var outputFile in config.Invokables.OutputToFiles)
            {
                var fullPath = Path.Combine(configFileDirectory, outputFile);
                var index = config.Invokables.OutputToFiles.IndexOf(outputFile);
                config.Invokables.OutputToFiles[index] = Path.GetFullPath(fullPath);
            }
        }

        return config;
    }
    
    public static void GenerateJSInvokable(Settings config, List<(string MethodName, string InvokableName)> invokables)
    {
        if (invokables.Count == 0)
        {
            return;
        }

        var jsBuilder = new StringBuilder();
        jsBuilder.AppendLine($"const {config.Invokables.JSClassName} = {{");
    
        foreach (var (methodName, invokableName) in invokables)
        {
            jsBuilder.AppendLine($"{methodName.Replace(".","_")}: \"{invokableName}\", ");
        }
            
        jsBuilder.AppendLine("};");

        if (config.Invokables.OutputToFiles.Count > 0)
        {
            foreach (var outputPath in config.Invokables.OutputToFiles)
            {
                File.WriteAllText(outputPath, jsBuilder.ToString());
            }
        }
    }
    
    private static bool IsConfigurationFile(AdditionalText text) => text.Path.EndsWith("RazorPageRoutes.json");
}