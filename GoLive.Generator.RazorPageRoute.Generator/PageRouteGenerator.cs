﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace GoLive.Generator.RazorPageRoute.Generator
{
    [Generator]
    public class PageRouteGenerator : ISourceGenerator
    {
        private GeneratorExecutionContext executionContext;
        private StringBuilder logBuilder;
        private static SymbolDisplayFormat symbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
        public void Initialize(GeneratorInitializationContext context)
        {
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}
        }

        public void Execute(GeneratorExecutionContext context)
        {
            executionContext = context;
            logBuilder = new StringBuilder();

            
            var config = LoadConfig(context, defaultNamespace:context.Compilation.Assembly.Name);

            if (config == null)
            {
                return;
            }

            try
            {
                logBuilder.AppendLine("Output file : " + config.OutputToFile);

                string source = Generate(config, context);
                logBuilder.AppendLine(source);
                logBuilder.AppendLine();
                logBuilder.AppendLine();
                logBuilder.AppendLine();
                logBuilder.AppendLine();
                logBuilder.AppendLine();

                if (!string.IsNullOrWhiteSpace(source))
                {
                    if (string.IsNullOrWhiteSpace(config.OutputToFile) && config.OutputToFiles.Count == 0)
                    {
                        context.AddSource("PageRoutes.g.cs", source);
                    }
                    else
                    {
                        if (config.OutputToFiles.Count > 0)
                        {
                            foreach (var configOutputToFile in config.OutputToFiles)
                            {
                                if (File.Exists(configOutputToFile))
                                {
                                    File.Delete(configOutputToFile);
                                }

                                File.WriteAllText(configOutputToFile, source);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(config.OutputToFile))
                        {
                            if (File.Exists(config.OutputToFile))
                            {
                                logBuilder.AppendLine("Output file exists, deleting");
                                File.Delete(config.OutputToFile);
                            }

                            File.WriteAllText(config.OutputToFile, source);
                        }
                    }
                }
                else
                {
                    ReportEmptyFile();
                    logBuilder.AppendLine("Source is empty");
                }
            }
            catch (Exception e) when (e.GetType() != typeof(IOException))
            {
                ReportError(e, Location.None);
                logBuilder.AppendLine(e.ToString());
                throw;
            }
            catch (Exception e)
            {
                ReportError(e);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(config.DebugOutputFile))
                {
                    File.WriteAllText(config.DebugOutputFile.Replace("(id)", Guid.NewGuid().ToString("N")), logBuilder.ToString());
                }
            }


            if (config.Invokables is { Enabled: true })
            {
                GenerateJSInvokable(config, context);
            }
        }

        private readonly DiagnosticDescriptor _errorRuleWithLog = new DiagnosticDescriptor("RPG0001", "RPG0001: Error in source generator", "Error in source generator<{0}>: '{1}'. Log file details: '{2}'.", "SourceGenerator", DiagnosticSeverity.Error, isEnabledByDefault: true);
        private readonly DiagnosticDescriptor _infoRule = new DiagnosticDescriptor("RPG0002", "RPG0002: Source code generated", "Source code generated<{0}>", "SourceGenerator", DiagnosticSeverity.Info, isEnabledByDefault: true);
        private readonly DiagnosticDescriptor _emptyFileRule = new DiagnosticDescriptor("RPG0003", "RPG0003: Source code output is empty", "Source code was not outputted", "SourceGenerator", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        private void ReportEmptyFile()
        {
            executionContext.ReportDiagnostic(Diagnostic.Create(_emptyFileRule, Location.None));
        }

        public void ReportInformation(Location? location = null)
        {
            if (location == null)
            {
                location = Location.None;
            }

            executionContext.ReportDiagnostic(Diagnostic.Create(_infoRule, location, GetType().Name));
        }

        public void ReportError(Exception e, Location? location = null)
        {
            if (location == null)
            {
                location = Location.None;
            }
            executionContext.ReportDiagnostic(Diagnostic.Create(_errorRuleWithLog, location, GetType().Name, e.Message));
        }


        private string Generate(Settings config, GeneratorExecutionContext context)
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
            
            logBuilder.AppendLine("Adding namespaces");

            var routes = context.Compilation.SyntaxTrees.Select(t => context.Compilation.GetSemanticModel(t)).Select(Scanner.ScanForPageRoutes).SelectMany(c => c).ToArray();
            logBuilder.AppendLine("Got Routes");
            source.AppendLine($"namespace {config.Namespace}");
            source.AppendOpenCurlyBracketLine();
            source.AppendLine($"public static partial class {config.ClassName}");
            source.AppendOpenCurlyBracketLine();

            if (routes.Length == 0)
            {
                logBuilder.AppendLine("Routes is 0 length");
                return null;
            }

            logBuilder.AppendLine("Foreach route");

            foreach (var pageRoute in routes)
            {
                var routeTemplate = TemplateParser.ParseTemplate(pageRoute.Route);

                string SlugName = Slug.Create(pageRoute.Route.Length > 1 ? string.Join(".", routeTemplate.Segments.Where(f => !f.IsParameter).Select(f => f.Value)) : "Home");

                if (string.IsNullOrEmpty(SlugName))
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

                if (pageRoute.QueryString is {Count: > 0})
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
            logBuilder.AppendLine("Done, returning");
            source.AppendCloseCurlyBracketLine();
            source.AppendCloseCurlyBracketLine();

            var outp = source.ToString();

            return outp;
        }
        
        
        private void OutputRouteExtensionMethod(SourceStringBuilder source, string SlugName, string parameterString, RouteTemplate routeTemplate, PageRoute pageRoute)
        {
            logBuilder.AppendLine("OutputRouteExtensionMethod");

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

        private void OutputRouteStringMethod(SourceStringBuilder source, string SlugName, string parameterString, RouteTemplate routeTemplate, PageRoute pageRoute)
        {
            logBuilder.AppendLine("Before creation #1");
            
            source.AppendLine($"public static string {SlugName} ({parameterString})");
            source.AppendOpenCurlyBracketLine();
            logBuilder.AppendLine("Foreach Param");

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

            source.AppendLine($"return url;");

            source.AppendCloseCurlyBracketLine();
        }

        private Settings LoadConfig(GeneratorExecutionContext context, string defaultNamespace)
        {
            var configFilePath = context.AdditionalFiles.FirstOrDefault(e => e.Path.ToLowerInvariant().EndsWith("razorpageroutes.json"));

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

            if (config.Invokables != null && (!string.IsNullOrWhiteSpace(config.Invokables.OutputToFile) || config.Invokables.OutputToFiles.Count > 0))
            {
                if (!string.IsNullOrWhiteSpace(config.Invokables.OutputToFile))
                {
                    var fullPath = Path.Combine(configFileDirectory, config.Invokables.OutputToFile);
                    config.Invokables.OutputToFile = Path.GetFullPath(fullPath);
                }

                foreach (var outputFile in config.Invokables.OutputToFiles)
                {
                    var fullPath = Path.Combine(configFileDirectory, outputFile);
                    var index = config.Invokables.OutputToFiles.IndexOf(outputFile);
                    config.Invokables.OutputToFiles[index] = Path.GetFullPath(fullPath);
                }
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
        
        
        private void GenerateJSInvokable(Settings config, GeneratorExecutionContext context)
        {
            var invokables = context.Compilation.SyntaxTrees.Select(t => context.Compilation.GetSemanticModel(t)).Select(Scanner.ScanForInvokables).SelectMany(c => c).ToArray();

            if (invokables.Length == 0)
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

            if (!string.IsNullOrWhiteSpace(config.Invokables.OutputToFile))
            {
                File.WriteAllText(config.Invokables.OutputToFile, jsBuilder.ToString());
            }

            if (config.Invokables.OutputToFiles.Count > 0)
            {
                foreach (var outputPath in config.Invokables.OutputToFiles)
                {
                    File.WriteAllText(outputPath, jsBuilder.ToString());
                }
            }
        }
    }
}