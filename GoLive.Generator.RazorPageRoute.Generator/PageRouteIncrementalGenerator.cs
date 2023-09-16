using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace GoLive.Generator.RazorPageRoute.Generator;

[Generator]
public class PageRouteIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<PageRoute> declarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (s, _) => Scanner.CanBeRazorPage(s),
                static (ctx, _) => GetRazorDeclarations(ctx))
            .Where(static c => c is not null)
            .SelectMany(static (c, _) => Scanner.ConvertToRoute(c));

        var configFiles = context.AdditionalTextsProvider.Where(IsConfigurationFile);
        var controllersAndConfig = declarations.Collect().Combine(configFiles.Collect());
        var defaultValuesCombined = context.AnalyzerConfigOptionsProvider.Combine(controllersAndConfig);
//context.RegisterSourceOutput(context.AdditionalTextsProvider, (spc, text) => Exec4(spc, text));
 //       context.RegisterSourceOutput(context.CompilationProvider, (spc, compilation) => Exec3(spc, compilation));
        
        context.RegisterSourceOutput(defaultValuesCombined, (spc, tuple) => Execute(spc, tuple));
    }

    private void Exec4(SourceProductionContext spc, AdditionalText text)
    {
        throw new NotImplementedException();
    }

    private void Exec3(SourceProductionContext spc, Compilation compilation)
    {
        throw new NotImplementedException();
    }


    private void Execute(SourceProductionContext context, (AnalyzerConfigOptionsProvider configProvider, (ImmutableArray<PageRoute> pageRoutes, ImmutableArray<AdditionalText> Config) Right) input)
    {
        _ = input.configProvider.GlobalOptions.TryGetValue("build_property.rootnamespace", out string defaultNamespace);
        
        var config = LoadConfig(input.Right.Config, defaultNamespace);
        
        var source = Generate(config, input.Right.pageRoutes);

        if (!string.IsNullOrWhiteSpace(source))
        {
            context.AddSource("PageRoutes.g.cs", source.ToString());
        }
        else
        {
            Console.WriteLine("Error");
        }
    }


    private string Generate(Settings config, IEnumerable<PageRoute> context)
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
        source.AppendLine($"public static class {config.ClassName}");
        source.AppendOpenCurlyBracketLine();

        if (!context.Any())
        {
            return null;
        }

        foreach (var pageRoute in context)
        {
            var routeTemplate = TemplateParser.ParseTemplate(pageRoute.Route);

            var SlugName = Slug.Create(pageRoute.Route.Length > 1 ? string.Join(".", routeTemplate.Segments.Where(f => !f.IsParameter).Select(f => f.Value)) : "Home");

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
                routeSegments.AddRange(pageRoute.QueryString.Select(prqp => $"{prqp.Type.ToDisplayString()}{(prqp.Type.IsReferenceType ? "?" : "")} {prqp.Name} = default"));
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

        var outp = source.ToString();

        return outp;
    }

    private void OutputRouteStringMethod(SourceStringBuilder source, string SlugName, string parameterString, RouteTemplate routeTemplate, PageRoute pageRoute)
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
                if (pageRouteQuerystringParameter.Type.ToDisplayString(symbolDisplayFormat) == "System.String")
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

    private void OutputRouteExtensionMethod(SourceStringBuilder source, string SlugName, string parameterString, RouteTemplate routeTemplate, PageRoute pageRoute)
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
                if (pageRouteQuerystringParameter.Type.ToDisplayString(symbolDisplayFormat) == "System.String")
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
    
    private static Settings LoadConfig(ImmutableArray<AdditionalText> configFiles, string defaultNamespace)
    {
        var configFilePath = configFiles.FirstOrDefault();

        if (configFilePath == null)
        {
            var settings = new Settings();
            settings.Namespace = defaultNamespace;
            settings.ClassName = "PageRoutes";
            return settings;
        }

        var jsonString = File.ReadAllText(configFilePath.Path);
        var config = JsonSerializer.Deserialize<Settings>(jsonString);
        var configFileDirectory = Path.GetDirectoryName(configFilePath.Path);

        if (string.IsNullOrEmpty(config.Namespace))
        {
            config.Namespace = defaultNamespace;
        }

        if (!string.IsNullOrWhiteSpace(config.OutputToFile))
        {
            var fullPath = Path.Combine(configFileDirectory, config.OutputToFile);
            config.OutputToFile = Path.GetFullPath(fullPath);
        }

        return config;
    }
    
    private static INamedTypeSymbol GetRazorDeclarations(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;
        var symbol = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

        return symbol is not null && Scanner.IsRoutableRazorComponent(symbol) ? symbol : null;
    }

    private static bool IsConfigurationFile(AdditionalText text)
    {
        return text.Path.EndsWith("RazorPageRoutes.json");
    }

    private static readonly SymbolDisplayFormat symbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
}