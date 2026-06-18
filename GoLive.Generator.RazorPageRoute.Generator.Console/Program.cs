// See https://aka.ms/new-console-template for more information
using GoLive.Generator.RazorPageRoute.Generator;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length < 1)
{
    Console.WriteLine("Parameters required to run - <Settings File>");
    return;
}

var settingsFile = args[0];
Console.WriteLine($"Running RPRG for settings: {settingsFile}");

var settings = BlazorRouteDiscoveryGenerator.ParseSettings(settingsFile, File.ReadAllText(settingsFile), null);
if (settings == null)
{
    Console.WriteLine("Failed to load settings file.");
    return;
}

// Get project root from settings file location
var projectRoot = Path.GetDirectoryName(Path.GetFullPath(settingsFile));

// Default Razor generated files directory
var defaultRazorPath = Path.Combine(projectRoot, "obj", "Generated", "Microsoft.CodeAnalysis.Razor.Compiler", "Microsoft.NET.Sdk.Razor.SourceGenerators.RazorSourceGenerator");

string razorPath = defaultRazorPath;
if (!Directory.Exists(razorPath))
{
    Console.WriteLine($"Razor generated files directory not found: {razorPath}");
    return;
}

Console.WriteLine($"Using Razor generated files directory: {razorPath}");

var generatedFiles = Directory.GetFiles(razorPath, "*.g.cs", SearchOption.AllDirectories);
var allRoutes = new List<PageRoute>();
var allInvokables = new List<Invokable>();

foreach (var generatedFile in generatedFiles)
{
    try
    {
        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(generatedFile));
        var root = tree.GetRoot();

        foreach (var cls in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            var routes = RouteExtractor.ExtractRoutes(cls);
            if (routes.Count > 0)
            {
                var queryParams = RouteExtractor.ExtractQueryParams(cls);
                var auth = RouteExtractor.ExtractAuth(cls, settings, (name, _) => null);
                var invokables = RouteExtractor.ExtractInvokables(cls);

                foreach (var route in routes)
                {
                    allRoutes.Add(new PageRoute(cls.Identifier.Text, route, queryParams, auth, invokables));
                }

                if (invokables.Count > 0)
                {
                    allInvokables.AddRange(invokables);
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error analyzing file {generatedFile}: {ex.Message}");
    }
}

// Remove duplicate routes by route string
allRoutes = allRoutes
    .GroupBy(r => r.Route)
    .Select(g => g.First())
    .ToList();

Console.WriteLine($"Discovered {allRoutes.Count} routes and {allInvokables.Count} JS invokables.");

CodeOutputter.GenerateOutput(settings, allRoutes);
CodeOutputter.GenerateJSInvokable(settings, allRoutes);

Console.WriteLine($"Route and JS invokable output generated to path: {string.Join(", ", settings.OutputToFiles)}");

return;
