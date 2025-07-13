using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoLive.Generator.RazorPageRoute.Generator.CodeReader;
using Microsoft.CodeAnalysis;

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
            string razorPath;

            if (generatedFolder == null || !Directory.Exists(generatedFolder))
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



            var discoveredRoutes = DiscoverRoutesFromGeneratedFiles(razorPath);

            var generatedCode = GenerateRouteRegistrationCode(discoveredRoutes);


        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("BRD003", "Route discovery error",
                $"Error during route discovery: {ex.Message}", "BlazorRouteDiscovery",
                DiagnosticSeverity.Error, true), Location.None));
        }
    }

    private void GenerateRouteRegistrationCode(List<PageRoute> Routes, Settings settings)
    {
        CodeOutputter.GenerateOutput(settings, Routes);

        if (settings.Invokables.Enabled)
        {
            CodeOutputter.GenerateJSInvokable(settings, );
        }
    }

    private string GetMSBuildProperty(GeneratorExecutionContext context, string propertyName)
    {
        return context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{propertyName}", out var value)
            ? value : null;
    }

    private List<PageRoute> DiscoverRoutesFromGeneratedFiles(string razorPath, Settings settings)
    {
        var generatedFiles = Directory.GetFiles(razorPath, "*.g.cs", SearchOption.AllDirectories);

        List<PageRoute> retr = new();

        foreach (var generatedFile in generatedFiles)
        {
            var analysisResult = SourceCodeAnalyzer.AnalyzeSourceFile(generatedFile);

            if (analysisResult.Classes.Count == 0)
            {
                continue; // Skip files with no classes
            }

            foreach (var @class in analysisResult.Classes)
            {
                var scanForPageRoutes = Scanner.ScanForPageRoutes(@class, settings);
                if (scanForPageRoutes != null && scanForPageRoutes.Any())
                {
                    retr.AddRange(scanForPageRoutes);
                }
            }
        }

        return retr;
    }

}