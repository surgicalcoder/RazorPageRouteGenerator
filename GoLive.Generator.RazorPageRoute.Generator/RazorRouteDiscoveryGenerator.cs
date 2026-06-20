using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GoLive.Generator.RazorPageRoute.Generator;

[Generator]
public class BlazorRouteDiscoveryGenerator : IIncrementalGenerator
{
    private const string diagnosticCategory = "BlazorRouteDiscovery";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configProvider = context.AdditionalTextsProvider
            .Where(static t => t.Path.EndsWith("RazorPageRoutes.json"))
            .Select(static (text, ct) => (Path: text.Path, Content: text.GetText(ct)?.ToString()))
            .Where(static t => t.Content != null);

        var optionsProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (options, ct) => (
                BaseIntermediateOutputPath: GetBuildProperty(options, "BaseIntermediateOutputPath"),
                RootNamespace: GetBuildProperty(options, "rootnamespace") ?? "DefaultNamespace"
            ));

        var combined = configProvider
            .Combine(optionsProvider)
            .Select(static (pair, ct) =>
            {
                var ((configPath, configContent), (baseIntermediateOutputPath, rootNamespace)) = pair;
                var settings = ParseSettings(configPath, configContent, rootNamespace);
                return (Settings: settings, BaseIntermediateOutputPath: baseIntermediateOutputPath);
            })
            .Where(static t => t.Settings != null)
            .Combine(context.CompilationProvider)
            .Select(static (pair, ct) =>
            {
                var ((settings, baseIntermediateOutputPath), compilation) = pair;

                var razorOutputPath = Path.Combine(
                    baseIntermediateOutputPath,
                    "Generated",
                    "Microsoft.CodeAnalysis.Razor.Compiler",
                    "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator");

                var routes = DiscoverRoutes(razorOutputPath, settings, compilation);

                return new GenerationResult(settings, routes, razorOutputPath);
            });

        context.RegisterSourceOutput(combined, (spc, result) =>
        {
            if (!Directory.Exists(result.RazorOutputPath))
            {
                spc.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("BRD002", "Razor generated files not found",
                        $"Razor generated files directory not found at: {result.RazorOutputPath}. "
                        + "Ensure EmitCompilerGeneratedFiles is enabled in the project. "
                        + "Routes will be generated on the next build.",
                        diagnosticCategory, DiagnosticSeverity.Warning, true),
                    Location.None));
                return;
            }

            var source = BuildSource(result.Settings, result.Routes);
            if (string.IsNullOrWhiteSpace(source))
                return;

            foreach (var outputPath in result.Settings.OutputToFiles)
            {
                File.WriteAllText(outputPath, source);
            }

            if (result.Settings.JsonRepresentation.Count > 0)
            {
                var json = JsonSerializer.Serialize(result.Routes, new JsonSerializerOptions { WriteIndented = true });
                foreach (var jsonPath in result.Settings.JsonRepresentation)
                {
                    File.WriteAllText(jsonPath, json);
                }
            }
        });
    }

    private static string GetBuildProperty(AnalyzerConfigOptionsProvider options, string propertyName)
    {
        options.GlobalOptions.TryGetValue($"build_property.{propertyName}", out var value);
        return value;
    }

    public static Settings ParseSettings(string configPath, string configContent, string rootNamespace)
    {
        var config = JsonSerializer.Deserialize<Settings>(configContent);
        if (config == null) return null;

        if (string.IsNullOrEmpty(config.Namespace))
        {
            config.Namespace = rootNamespace;
        }

        var configFileDirectory = Path.GetDirectoryName(configPath);

        if (config.OutputToFiles != null && config.OutputToFiles.Count > 0)
        {
            config.OutputToFiles = config.OutputToFiles.Select(r =>
                Path.GetFullPath(Path.Combine(configFileDirectory, r))).ToList();
        }

        if (config.JsonRepresentation != null && config.JsonRepresentation.Count > 0)
        {
            config.JsonRepresentation = config.JsonRepresentation.Select(r =>
                Path.GetFullPath(Path.Combine(configFileDirectory, r))).ToList();
        }

        if (config.Invokables != null && config.Invokables.OutputToFiles.Count > 0)
        {
            for (int i = 0; i < config.Invokables.OutputToFiles.Count; i++)
            {
                config.Invokables.OutputToFiles[i] =
                    Path.GetFullPath(Path.Combine(configFileDirectory, config.Invokables.OutputToFiles[i]));
            }
        }

        return config;
    }

    private static List<PageRoute> DiscoverRoutes(string razorPath, Settings settings, Compilation compilation)
    {
        if (!Directory.Exists(razorPath))
        {
            return [];
        }

        var generatedFiles = Directory.GetFiles(razorPath, "*.g.cs", SearchOption.AllDirectories);
        List<PageRoute> retr = [];

        foreach (var generatedFile in generatedFiles)
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(generatedFile));
            var root = tree.GetRoot();

            foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var routes = RouteExtractor.ExtractRoutes(cls);
                if (routes.Count == 0) continue;

                var queryParams = RouteExtractor.ExtractQueryParams(cls);
                var auth = RouteExtractor.ExtractAuth(cls, settings,
                    (name, usings) => ResolveConst(name, usings, compilation));
                var invokables = RouteExtractor.ExtractInvokables(cls);

                foreach (var route in routes)
                {
                    retr.Add(new PageRoute(cls.Identifier.Text, route, queryParams, auth, invokables));
                }
            }
        }

        retr = retr.GroupBy(r => r.Route).Select(g => g.First()).ToList();
        return retr;
    }

    private static string ResolveConst(string constName, List<string> namespaceImports, Compilation compilation)
    {
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();

            foreach (var fieldDecl in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                if (!fieldDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword)))
                    continue;

                foreach (var variable in fieldDecl.Declaration.Variables)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                    if (symbol == null || !symbol.HasConstantValue)
                        continue;

                    if (symbol.Name == constName || symbol.ToDisplayString() == constName)
                    {
                        if (namespaceImports == null || namespaceImports.Count == 0 ||
                            namespaceImports.Contains(symbol.ContainingNamespace.ToDisplayString()))
                        {
                            return symbol.ConstantValue.ToString();
                        }
                    }

                    if (namespaceImports != null)
                    {
                        foreach (var nsImport in namespaceImports)
                        {
                            if (symbol.ToDisplayString().EndsWith($"{nsImport}.{constName}"))
                            {
                                return symbol.ConstantValue.ToString();
                            }
                        }
                    }
                }
            }
        }

        foreach (var assembly in compilation.References
                     .Select(r => compilation.GetAssemblyOrModuleSymbol(r))
                     .OfType<IAssemblySymbol>())
        {
            foreach (var nsImport in namespaceImports ?? [])
            {
                var ns = assembly.GlobalNamespace.GetNamespaceMembers()
                    .FirstOrDefault(n => n.ToDisplayString() == nsImport);
                if (ns == null) continue;

                foreach (var type in ns.GetTypeMembers())
                {
                    foreach (var member in type.GetMembers())
                    {
                        if (member is IFieldSymbol fs && fs.IsConst &&
                            (fs.Name == constName || fs.ToDisplayString() == constName))
                        {
                            return fs.ConstantValue.ToString();
                        }
                    }
                }
            }
        }

        return null;
    }

    private static string BuildSource(Settings settings, List<PageRoute> routes)
    {
        var source = new SourceStringBuilder();

        if (settings.OutputLastCreatedTime)
        {
            source.AppendLine($"// This file was generated on {DateTime.Now:R}");
        }

        source.AppendLine("using System;");
        source.AppendLine("using System.Net.Http;");
        source.AppendLine("using System.Threading.Tasks;");
        source.AppendLine("using System.Net.Http.Json;");
        source.AppendLine("using System.Collections.Generic;");

        if (settings.OutputIAuthorizeData)
        {
            source.AppendLine("using Microsoft.AspNetCore.Authorization;");
        }

        if (settings.OutputIAuthorizeData || settings.OutputExtensionMethod)
        {
            source.AppendLine("using Microsoft.AspNetCore.Components;");
        }

        source.AppendLine($"namespace {settings.Namespace}");
        source.AppendOpenCurlyBracketLine();
        source.AppendLine($"public static partial class {settings.ClassName}");
        source.AppendOpenCurlyBracketLine();

        if (routes.Count == 0)
        {
            return source.ToString();
        }

        List<string> completedAuthData = [];
        foreach (var pageRoute in routes)
        {
            AppendRoute(source, pageRoute, settings, completedAuthData);
        }

        source.AppendCloseCurlyBracketLine();
        source.AppendCloseCurlyBracketLine();

        return source.ToString();
    }

    private static void AppendRoute(SourceStringBuilder source, PageRoute pageRoute, Settings settings, List<string> completedAuthData)
    {
        var routeTemplate = Routing.TemplateParser.ParseTemplate(pageRoute.Route);

        var slugName = pageRoute.Route.Length > 1
            ? Slug.Create(string.Join(".", routeTemplate.Segments.Where(f => !f.IsParameter).Select(f => f.Value)), new SlugOptions { ToLower = false })
            : "Home";

        if (string.IsNullOrWhiteSpace(slugName))
        {
            slugName = pageRoute.Name;
        }

        var routeSegments = routeTemplate.Segments
            .Where(e => e.IsParameter)
            .Select(segment =>
            {
                var constraint = segment.Constraints.Any()
                    ? segment.Constraints.FirstOrDefault().GetConstraintType()
                    : null;

                if (constraint == null)
                    return $"string {segment.Value}";

                return segment.IsOptional
                    ? $"{constraint.FullName}? {segment.Value}"
                    : $"{constraint.FullName} {segment.Value}";
            }).ToList();

        if (pageRoute.QueryString is { Count: > 0 })
        {
            routeSegments.AddRange(pageRoute.QueryString
                .Select(prqp => $"{prqp.Type} {prqp.Name} = default"));
        }

        var parameterString = string.Join(", ", routeSegments);

        AppendRouteMethod(source, slugName, parameterString, routeTemplate, pageRoute);

        if (settings.OutputExtensionMethod)
        {
            AppendExtensionMethod(source, slugName, parameterString, routeTemplate, pageRoute, settings);
            if (!completedAuthData.Contains(slugName))
            {
                completedAuthData.Add(slugName);
                AppendAuthDataClass(source, slugName, pageRoute, settings);
            }
        }
    }

    private static void AppendRouteMethod(SourceStringBuilder source, string slugName, string parameterString,
        Routing.RouteTemplate routeTemplate, PageRoute pageRoute)
    {
        source.AppendLine($"public static string {slugName} ({parameterString})");
        source.AppendOpenCurlyBracketLine();

        if (routeTemplate.Segments.Any(e => e.IsParameter))
        {
            source.AppendIndent();
            source.Append("string url = $\"", false);
            foreach (var seg in routeTemplate.Segments)
            {
                source.Append(seg.IsParameter ? $"/{{{seg.Value}.ToString()}}" : $"/{seg.Value}", false);
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
            foreach (var prqp in pageRoute.QueryString)
            {
                if (prqp.Type == "System.String" || prqp.Type.Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    source.AppendLine($"if (!string.IsNullOrWhiteSpace({prqp.Name})) ");
                }
                else
                {
                    source.AppendLine($"if ({prqp.Name} != default) ");
                }
                source.AppendOpenCurlyBracketLine();
                source.AppendLine($"queryString.Add(\"{prqp.Name}\", {prqp.Name}.ToString());");
                source.AppendCloseCurlyBracketLine();
            }
            source.AppendLine("");
            source.AppendLine("url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);");
        }

        source.AppendLine("return url;");
        source.AppendCloseCurlyBracketLine();
    }

    private static void AppendExtensionMethod(SourceStringBuilder source, string slugName, string parameterString,
        Routing.RouteTemplate routeTemplate, PageRoute pageRoute, Settings settings)
    {
        if (pageRoute.Auth is { RequiresAuthentication: true })
        {
            source.AppendLine("/// <summary>");
            source.AppendLine($"/// Page Requires Authentication{(pageRoute.Auth.CustomAuth != null ? ", Custom Authentication Provider (CustomAuth)" : "")}");
            if (pageRoute.Auth.Roles is { Count: > 0 })
                source.AppendLine($"/// Roles: {string.Join(", ", pageRoute.Auth.Roles)}");
            if (pageRoute.Auth.Policies is { Count: > 0 })
                source.AppendLine($"/// Policies: {string.Join(", ", pageRoute.Auth.Policies)}");
            if (pageRoute.Auth.CustomAuth != null)
            {
                foreach (var ca in pageRoute.Auth.CustomAuth)
                {
                    source.AppendLine($"/// Custom Authentication ProviderName: {ca.Name}");
                    source.AppendLine($"/// Custom Auth Constructor Params: {string.Join(", ", ca.CtorParams)}");
                    source.AppendLine($"/// Custom Auth Named Params: {string.Join(", ", ca.NamedParams)}");
                }
            }
            source.AppendLine("/// </summary>");
        }

        if (string.IsNullOrWhiteSpace(parameterString))
        {
            source.AppendLine($"public static void {slugName} (this NavigationManager manager, bool forceLoad = false, bool replace=false)");
        }
        else
        {
            source.AppendLine($"public static void {slugName} (this NavigationManager manager, {parameterString}, bool forceLoad = false, bool replace=false)");
        }

        source.AppendOpenCurlyBracketLine();

        if (routeTemplate.Segments.Any(e => e.IsParameter))
        {
            source.AppendIndent();
            source.Append("string url = $\"", false);
            foreach (var seg in routeTemplate.Segments)
            {
                source.Append(seg.IsParameter ? $"/{{{seg.Value}.ToString()}}" : $"/{seg.Value}", false);
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
            foreach (var prqp in pageRoute.QueryString)
            {
                if (prqp.Type == "System.String" || prqp.Type.Equals("string", StringComparison.OrdinalIgnoreCase))
                {
                    source.AppendLine($"if (!string.IsNullOrWhiteSpace({prqp.Name})) ");
                }
                else
                {
                    source.AppendLine($"if ({prqp.Name} != default) ");
                }
                source.AppendOpenCurlyBracketLine();
                source.AppendLine($"queryString.Add(\"{prqp.Name}\", {prqp.Name}.ToString());");
                source.AppendCloseCurlyBracketLine();
            }
            source.AppendLine("");
            source.AppendLine("url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(url, queryString);");
        }

        source.AppendLine("manager.NavigateTo(url, forceLoad, replace);");
        source.AppendCloseCurlyBracketLine();
    }

    private static void AppendAuthDataClass(SourceStringBuilder source, string slugName, PageRoute pageRoute, Settings settings)
    {
        if (pageRoute.Auth is not { RequiresAuthentication: true } || !settings.OutputIAuthorizeData)
            return;

        source.AppendLine($"public class {slugName}_AuthData : IAuthorizeData");
        source.AppendOpenCurlyBracketLine();

        if (pageRoute.Auth.CustomAuth is { Count: > 0 })
        {
            var authItem = pageRoute.Auth.CustomAuth.FirstOrDefault();
            var authCfg = settings.Auth.FirstOrDefault(a =>
                a.Attribute != null && a.Attribute.Contains(authItem.Name));

            if (authCfg != null)
            {
                var policyList = EvaluateCode(authCfg.PolicyTransformer, authItem);
                var rolesList = EvaluateCode(authCfg.RolesTransformer, authItem);
                var authSchemesList = EvaluateCode(authCfg.AuthenticationSchemeTransformer, authItem);

                source.AppendLine($"public string Policy {{ get; set; }} = {(policyList?.Count > 0 ? $"\"{string.Join(", ", policyList.Select(QuoteLiteral))}\"" : "String.Empty")}; ");
                source.AppendLine($"public string Roles {{ get; set; }} = {(rolesList?.Count > 0 ? $"\"{string.Join(", ", rolesList.Select(QuoteLiteral))}\"" : "String.Empty")}; ");
                source.AppendLine($"public string AuthenticationSchemes {{ get; set; }} = {(authSchemesList?.Count > 0 ? $"\"{string.Join(", ", authSchemesList.Select(QuoteLiteral))}\"" : "String.Empty")}; ");
            }
        }
        else
        {
            source.AppendLine($"public string Policy {{ get; set; }} = {(pageRoute.Auth.Policies?.Count > 0 ? $"\"{string.Join(",", pageRoute.Auth.Policies)}\"" : "String.Empty")};");
            source.AppendLine($"public string Roles {{ get; set; }} = {(pageRoute.Auth.Roles?.Count > 0 ? $"\"{string.Join(",", pageRoute.Auth.Roles)}\"" : "String.Empty")}; ");
            source.AppendLine($"public string AuthenticationSchemes {{ get; set; }} = {(pageRoute.Auth.AuthenticationSchemes?.Count > 0 ? $"\"{string.Join(", ", pageRoute.Auth.AuthenticationSchemes)}\"" : "String.Empty")}; ");
        }

        source.AppendCloseCurlyBracketLine();
    }

    private static List<string> EvaluateCode(string transformer, PageRouteAuthCustomAuth authItem)
    {
        if (string.IsNullOrWhiteSpace(transformer))
            return null;

        try
        {
            var evaluator = new Data.Eval.Evaluator(transformer);
            evaluator.AddUsing("System.Collections.Generic");
            evaluator["ConstructorParameters"] = authItem.CtorParams;
            evaluator["NamedParameters"] = authItem.NamedParams;
            return evaluator.Eval<List<string>>();
        }
        catch (Exception e)
        {
            return [e.ToString()];
        }
    }

    private static string QuoteLiteral(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

public record struct GenerationResult(Settings Settings, List<PageRoute> Routes, string RazorOutputPath);
