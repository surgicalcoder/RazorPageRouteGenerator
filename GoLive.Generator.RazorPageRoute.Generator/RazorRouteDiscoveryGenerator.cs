using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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

            var configFiles = context.AdditionalFiles.Where(IsConfigurationFile);
            var defaultNamespace = GetMSBuildProperty(context, "rootnamespace") ?? "DefaultNamespace";
            var settings = LoadConfig(configFiles, defaultNamespace);

            var discoveredRoutes = DiscoverRoutesFromGeneratedFiles(razorPath, settings);

            GenerateRouteCode(discoveredRoutes, settings);
            GenerateInvokablesCode(settings, discoveredRoutes);

        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("BRD003", "Route discovery error",
                $"Error during route discovery: {ex}", "BlazorRouteDiscovery",
                DiagnosticSeverity.Error, true), Location.None));
        }
    }

    private void GenerateInvokablesCode(Settings config, List<PageRoute> discoveredRoutes)
    {
        var invokables = discoveredRoutes.Where(r=>r.Invokables != null && r.Invokables.Any()).SelectMany(r => r.Invokables).ToList();
        if (invokables == null || invokables.Count == 0)
        {
            return;
        }

        var jsBuilder = new StringBuilder();
        jsBuilder.AppendLine($"const {config.Invokables.JSClassName} = {{");

        foreach (var invokable in invokables)
        {
            jsBuilder.AppendLine($"{invokable.MethodName.Replace(".", "_")}: \"{invokable.InvokableName}\", ");
        }

        jsBuilder.AppendLine("};");

        if (config.Invokables.OutputToFiles != null && config.Invokables.OutputToFiles.Count > 0)
        {
            foreach (var outputPath in config.Invokables.OutputToFiles)
            {
                File.WriteAllText(outputPath, jsBuilder.ToString());
            }
        }
    }

    private void GenerateRouteCode(List<PageRoute> Routes, Settings settings)
    {
        CodeOutputter.GenerateOutput(settings, Routes);

        if (settings.Invokables.Enabled)
        {
            CodeOutputter.GenerateJSInvokable(settings, Routes);
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

        List<PageRoute> retr = [];

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

        // Remove duplicates by route
        retr = retr
            .GroupBy(r => r.Route)
            .Select(g => g.First())
            .ToList();

        return retr;
    }

    private static bool IsConfigurationFile(AdditionalText text) => text.Path.EndsWith("RazorPageRoutes.json");
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
}