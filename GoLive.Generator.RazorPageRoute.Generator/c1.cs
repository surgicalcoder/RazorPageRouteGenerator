using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GoLive.Generator.RazorPageRoute.Generator;

[Generator]
public class PageRouteIncrementalExperimentalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabled = context.AnalyzerConfigOptionsProvider.Select((provider, _) =>
        {
            if (!provider.GlobalOptions.TryGetValue("build_property.EmitCompilerGeneratedFiles", out string wibble))
            {
                return null;
            }

            if (wibble.ToLowerInvariant() != "true")
            {
                return null;
            }

            if (!provider.GlobalOptions.TryGetValue("build_property.projectdir", out string projectDir))
            {
                return null;
            }

            if (!provider.GlobalOptions.TryGetValue("build_property.compilergeneratedfilesoutputpath", out string generatedLocation))
            {
                return null;
            }

            return Path.Combine(projectDir, generatedLocation);

        }).Select((s, _) =>
        {
            if (s == null)
            {
                return null;
            }

            string rootFolderPath = Path.Combine(s, "Microsoft.NET.Sdk.Razor.SourceGenerators\\Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator");
            foreach (var file in Directory.GetFiles(rootFolderPath, "*.g.cs"))
            {
                var parsed =CSharpSyntaxTree.ParseText(File.ReadAllText(file));
                var root = parsed.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                var wibble = Scanner.ScanForPageRoutesIncremental(root);

                if (wibble != null && wibble.Any())
                {
                    return wibble;
                }
            }
            return null;
        });
        
        
        /*context.RegisterSourceOutput(context.MetadataReferencesProvider.Collect(), (productionContext, array) => Console.WriteLine(array.Length) );
        context.RegisterSourceOutput(context.AnalyzerConfigOptionsProvider, (productionContext, array) =>
        {
            Dictionary<string, string> blarg = new();
            foreach (var globalOptionsKey in array.GlobalOptions.Keys)
            {
                if (array.GlobalOptions.TryGetValue(globalOptionsKey, out var wibble))
                {
                    blarg.Add(globalOptionsKey, wibble);
                }
            }

            Console.WriteLine("blarg");
        });*/
    }
}