// See https://aka.ms/new-console-template for more information

using GoLive.Generator.RazorPageRoute.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length < 3)
{
    Console.WriteLine("Parameters required to run - <Settings File> <Generated Location> <Namespace>");
    return;
}

var settingsFile = args[0];
var generatedLocation = args[1];
var @namespace = args[2];

var settings = PageRouteIncrementalExperimentalGenerator.LoadConfigFromFile(settingsFile, @namespace);
var routes = GetPageRoutes(generatedLocation);
PageRouteIncrementalExperimentalGenerator.GenerateOutput(default, settings, routes);

List<PageRoute> GetPageRoutes(string GeneratedPath)
{
    List<PageRoute> retr = new();
    var rootFolderPath = Path.Combine(GeneratedPath, "Microsoft.NET.Sdk.Razor.SourceGenerators\\Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator");

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

    return retr;
}