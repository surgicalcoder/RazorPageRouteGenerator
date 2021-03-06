using System;
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
                    if (string.IsNullOrWhiteSpace(config.OutputToFile))
                    {
                        context.AddSource("PageRoutes.g.cs", source);
                    }
                    else
                    {
                        if (File.Exists(config.OutputToFile))
                        {
                            logBuilder.AppendLine("Output file exists, deleting");
                            File.Delete(config.OutputToFile);
                        }

                        File.WriteAllText(config.OutputToFile, source);
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

                if (!string.IsNullOrWhiteSpace(config.DebugOutputFile))
                {
                    File.WriteAllText(config.DebugOutputFile.Replace("(id)", Guid.NewGuid().ToString("N")), logBuilder.ToString());
                }

                throw;
            }
            catch (Exception e)
            {
                ReportError(e);
            }
            finally
            {
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
            executionContext.ReportDiagnostic(Diagnostic.Create(_errorRuleWithLog, location, GetType().Name, e.Message, "g:\\scratch\\source-code-log-path.txt"));
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
            logBuilder.AppendLine("Adding namespaces");

            var routes = context.Compilation.SyntaxTrees.Select(t => context.Compilation.GetSemanticModel(t)).Select(Scanner.ScanForPageRoutes).SelectMany(c => c).ToArray();
            logBuilder.AppendLine("Got Routes");
            source.AppendLine($"namespace {config.Namespace}");
            source.AppendOpenCurlyBracketLine();
            source.AppendLine($"public static class {config.ClassName}");
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

                var parameterString = string.Join(", ", routeTemplate.Segments.Where(e => e.IsParameter).Select(delegate (TemplateSegment segment)
                {
                    var constraint = segment.Constraints.Any() ? segment.Constraints.FirstOrDefault().GetConstraintType() : null;

                    if (constraint == null)
                    {
                        return $"string {segment.Value}";
                    }
                    else
                    {
                        return segment.IsOptional ? $"{constraint.FullName}? {segment.Value}" : $"{constraint.FullName} {segment.Value}";
                    }
                }));

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
                    source.AppendLine($"return url;");
                }

                else
                {
                    source.AppendLine($"string url = \"{pageRoute.Route}\";");
                    source.AppendLine($"return url;");
                }

                source.AppendCloseCurlyBracketLine();
            }
            logBuilder.AppendLine("Done, returning");
            source.AppendCloseCurlyBracketLine();
            source.AppendCloseCurlyBracketLine();

            var outp = source.ToString();

            return outp;
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

            return config;
        }
    }
}