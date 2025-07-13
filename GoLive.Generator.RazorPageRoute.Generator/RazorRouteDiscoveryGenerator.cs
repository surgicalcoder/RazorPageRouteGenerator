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
            AnalysisResult analysisResult = SourceCodeAnalyzer.AnalyzeSourceFile(generatedFile);

            if (analysisResult.Classes.Count == 0)
            {
                continue; // Skip files with no classes
            }



            Console.WriteLine(analysisResult.Classes.Count);
        }



        return Array.Empty<RouteInfo>().ToList();
    }

}