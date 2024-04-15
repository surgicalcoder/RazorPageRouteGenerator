#region

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#endregion

namespace GoLive.Generator.RazorPageRoute.Generator;

[Generator]
public class PageRouteIncrementalExperimentalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var defaultNamespace = context.AnalyzerConfigOptionsProvider.Select((provider, _) => !provider.GlobalOptions.TryGetValue("build_property.rootnamespace", out var ns) ? "DefaultNamespace" : ns);
        
        var items = context.AnalyzerConfigOptionsProvider.Select((provider, _) =>
        {
            if (!provider.GlobalOptions.TryGetValue("build_property.EmitCompilerGeneratedFiles", out var wibble))
            {
                return null;
                //return "ERROR: build_property.EmitCompilerGeneratedFiles not set";
            }

            if (wibble.ToLowerInvariant() != "true")
            {
                return null;
                //return $"ERROR: build_property.EmitCompilerGeneratedFiles is not true (value is {wibble.ToLowerInvariant()})";
            }

            if (!provider.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir))
            {
                return null;
                //return $"ERROR: build_property.projectdir is not set";
            }

            if (!provider.GlobalOptions.TryGetValue("build_property.compilergeneratedfilesoutputpath", out var generatedLocation))
            {
                return null;
                //return $"ERROR: build_property.compilergeneratedfilesoutputpath is not set";
            }

            return Path.Combine(projectDir, generatedLocation);
        }).Select((s, _) =>
        {
            if (s == null)
            {
                return null;
            }

            List<PageRoute> retr = new();
            var rootFolderPath = Path.Combine(s, "Microsoft.NET.Sdk.Razor.SourceGenerators\\Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator");

            foreach (var file in Directory.GetFiles(rootFolderPath, "*.g.cs"))
            {
                var parsed = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
                var root = parsed.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                var pageRoutes = Scanner.ScanForPageRoutesIncremental(root);

                if (pageRoutes != null && pageRoutes.Any())
                {
                    retr.AddRange(pageRoutes);
                }
            }

            return retr.DistinctBy(e=>e.Route).ToList();
        });

        var configFiles = context.AdditionalTextsProvider.Where(IsConfigurationFile);

        context.RegisterSourceOutput(items.Combine(configFiles.Collect()).Combine(defaultNamespace), (productionContext, routes) => Output(productionContext, routes));
    }

    private void Output(SourceProductionContext productionContext, ((List<PageRoute> Left, ImmutableArray<AdditionalText> Right) Left, string defaultNamespace) input)
    {
        var config = LoadConfig(input.Left.Right, input.defaultNamespace);

        GenerateOutput(productionContext, config, input.Left.Left);
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
                //routeSegments.AddRange(pageRoute.QueryString.Select(prqp => $"{prqp.Type}{(prqp.Type.IsReferenceType ? "?" : "")} {prqp.Name} = default"));
            }

            var parameterString = string.Join(", ", routeSegments);

            OutputRouteStringMethod(source, SlugName, parameterString, routeTemplate, pageRoute);

            if (config.OutputExtensionMethod)
            {
                OutputRouteExtensionMethod(source, SlugName, parameterString, routeTemplate, pageRoute);
            }
        }

        source.AppendCloseCurlyBracketLine();
        source.AppendCloseCurlyBracketLine();

        var sourceOutput = source.ToString();

        if (!string.IsNullOrWhiteSpace(sourceOutput))
        {
            if (string.IsNullOrWhiteSpace(config.OutputToFile) && config.OutputToFiles.Count == 0)
            {
                productionContext.AddSource("PageRoutes.g.cs", sourceOutput);
            }
            else
            {
                if (config.OutputToFiles.Count > 0)
                {
                    foreach (var configOutputToFile in config.OutputToFiles)
                    {
                        /*if (File.Exists(configOutputToFile))
                        {
                            File.Delete(configOutputToFile);
                        }*/

                        File.WriteAllText(configOutputToFile, sourceOutput);
                    }
                }

                if (!string.IsNullOrWhiteSpace(config.OutputToFile))
                {
                    /*if (File.Exists(config.OutputToFile))
                    {
                        File.Delete(config.OutputToFile);
                    }*/

                    File.WriteAllText(config.OutputToFile, sourceOutput);
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


    private static void OutputRouteExtensionMethod(SourceStringBuilder source, string SlugName, string parameterString, RouteTemplate routeTemplate, PageRoute pageRoute)
    {
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

        if (!string.IsNullOrWhiteSpace(config.OutputToFile))
        {
            var fullPath = Path.Combine(configFileDirectory, config.OutputToFile);
            config.OutputToFile = Path.GetFullPath(fullPath);
        }

        if (config.OutputToFiles != null && config.OutputToFiles.Any())
        {
            config.OutputToFiles = config.OutputToFiles.Select(r =>
            {
                var fullPath = Path.Combine(configFileDirectory, r);

                return fullPath;
            }).ToList();
        }

        return config;
    }


    private static bool IsConfigurationFile(AdditionalText text)
    {
        return text.Path.EndsWith("RazorPageRoutes.json");
    }
}