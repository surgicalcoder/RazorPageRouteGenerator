using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GoLive.Generator.RazorPageRoute.Generator.CodeReader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace GoLive.Generator.RazorPageRoute.Generator;
[Generator]
public class BlazorRouteDiscoveryGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization required
    }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            // Get BaseIntermediateOutputPath from MSBuild properties
            var baseIntermediateOutputPath = GetMSBuildProperty(context, "BaseIntermediateOutputPath");
            if (string.IsNullOrEmpty(baseIntermediateOutputPath))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor("BRD001", "Missing BaseIntermediateOutputPath",
                    "BaseIntermediateOutputPath MSBuild property not found", "BlazorRouteDiscovery",
                    DiagnosticSeverity.Warning, true), Location.None));
                return;
            }

           

            var generatedFolder = Path.Combine(GetMSBuildProperty(context, "projectdir"), GetMSBuildProperty(context, "CompilerGeneratedFilesOutputPath"));

            var highestFolder = Scanner.getHighestFolderVersion(Path.Combine(baseIntermediateOutputPath, "Debug"));
            // Construct path to Razor generated files
            string razorPath;

            if (generatedFolder == null && !Directory.Exists(generatedFolder))
            {
                razorPath = Path.Combine(highestFolder, "Razor");

                if (!Directory.Exists(razorPath))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("BRD002", "Razor directory not found",
                            $"Razor directory not found at: {razorPath}", "BlazorRouteDiscovery",
                            DiagnosticSeverity.Warning, true), Location.None));
                    return;
                }
            }
            else
            {
                razorPath = generatedFolder;
            }



                // Discover routes from .g.cs files
                var discoveredRoutes = DiscoverRoutesFromGeneratedFiles(razorPath);

            // Generate route registration code
            var generatedCode = GenerateRouteRegistrationCode(discoveredRoutes);

            // Add the generated source to the compilation
            context.AddSource("BlazorRouteDiscovery.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("BRD003", "Route discovery error",
                $"Error during route discovery: {ex.Message}", "BlazorRouteDiscovery",
                DiagnosticSeverity.Error, true), Location.None));
        }
    }

    private string GetMSBuildProperty(GeneratorExecutionContext context, string propertyName)
    {
        return context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{propertyName}", out var value)
            ? value : null;
    }

    private List<RouteInfo> DiscoverRoutesFromGeneratedFiles(string razorPath)
    {

        var generatedFiles = Directory.GetFiles(razorPath, "*.g.cs", SearchOption.AllDirectories);

        foreach (var generatedFile in generatedFiles)
        {
            var res1 = SourceCodeAnalyzer.AnalyzeSourceFile(generatedFile);

            Console.WriteLine(res1.Classes.Count);
        }

        
        /*var routes = new List<RouteInfo>();
        var generatedFiles = Directory.GetFiles(razorPath, "*.g.cs", SearchOption.AllDirectories);

        // Regex patterns to match RouteAttribute declarations
        var routeAttributePattern = new Regex(
            @"\[global::Microsoft\.AspNetCore\.Components\.RouteAttribute\(""([^""]+)""\)\]",
            RegexOptions.Compiled | RegexOptions.Multiline);

        var classNamePattern = new Regex(
            @"public\s+partial\s+class\s+(\w+)\s*:",
            RegexOptions.Compiled | RegexOptions.Multiline);

        foreach (var filePath in generatedFiles)
        {
            try
            {
                var fileContent = File.ReadAllText(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);

                // Extract class name
                var classMatch = classNamePattern.Match(fileContent);
                var className = classMatch.Success ? classMatch.Groups[1].Value : fileName.Replace(".razor.g", "");

                // Extract all route attributes
                var routeMatches = routeAttributePattern.Matches(fileContent);

                foreach (Match match in routeMatches)
                {
                    var routeTemplate = match.Groups[1].Value;
                    routes.Add(new RouteInfo
                    {
                        Template = routeTemplate,
                        ComponentName = className,
                        SourceFile = Path.GetFileName(filePath)
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error but continue processing other files
                continue;
            }
        }*/

        return Array.Empty<RouteInfo>().ToList();
    }

    private string GenerateRouteRegistrationCode(List<RouteInfo> routes)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("// This code was generated by BlazorRouteDiscoveryGenerator");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using Microsoft.AspNetCore.Components;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine();
        sb.AppendLine("namespace BlazorApp.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    public static class DiscoveredRoutes");
        sb.AppendLine("    {");
        sb.AppendLine("        public static readonly RouteInfo[] Routes = new RouteInfo[]");
        sb.AppendLine("        {");

        foreach (var route in routes.OrderBy(r => r.Template))
        {
            sb.AppendLine($"            new RouteInfo {{ Template = \"{route.Template}\", ComponentName = \"{route.ComponentName}\", SourceFile = \"{route.SourceFile}\" }},");
        }

        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        public static void RegisterRoutes(IServiceCollection services)");
        sb.AppendLine("        {");
        sb.AppendLine("            foreach (var route in Routes)");
        sb.AppendLine("            {");
        sb.AppendLine("                // Route registration logic can be added here");
        sb.AppendLine("                // services.AddSingleton(route);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public static void PrintDiscoveredRoutes()");
        sb.AppendLine("        {");
        sb.AppendLine("            Console.WriteLine($\"Discovered {Routes.Length} routes:\");");
        sb.AppendLine("            foreach (var route in Routes)");
        sb.AppendLine("            {");
        sb.AppendLine("                Console.WriteLine($\"  {route.Template} -> {route.ComponentName} (from {route.SourceFile})\");");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public class RouteInfo");
        sb.AppendLine("    {");
        sb.AppendLine("        public string Template { get; set; } = string.Empty;");
        sb.AppendLine("        public string ComponentName { get; set; } = string.Empty;");
        sb.AppendLine("        public string SourceFile { get; set; } = string.Empty;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private class RouteInfo
    {
        public string Template { get; set; } = string.Empty;
        public string ComponentName { get; set; } = string.Empty;
        public string SourceFile { get; set; } = string.Empty;
    }
}