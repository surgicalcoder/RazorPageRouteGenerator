// See https://aka.ms/new-console-template for more information
using GoLive.Generator.RazorPageRoute.Generator;
using GoLive.Generator.RazorPageRoute.Generator.CodeReader;


if (args.Length < 1)
{
    Console.WriteLine("Parameters required to run - <Settings File>");
    return;
}

var settingsFile = args[0];
Console.WriteLine($"Running RPRG for settings: {settingsFile}");

var settings = BlazorRouteDiscoveryGenerator.LoadConfigFromFile(settingsFile, null);
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

var generatedFiles = Directory.GetFiles(razorPath, "*.cs", SearchOption.AllDirectories);
var allRoutes = new List<PageRoute>();
var allInvokables = new List<Invokable>();

foreach (var generatedFile in generatedFiles)
{
    try
    {
        var analysisResult = SourceCodeAnalyzer.AnalyzeSourceFile(generatedFile, (constName, nsImports) => null);
        foreach (var @class in analysisResult.Classes)
        {
            var routes = Scanner.ScanForPageRoutes(@class, settings);
            if (routes != null && routes.Any())
            {
                allRoutes.AddRange(routes);
            }
            var invokables = Scanner.ScanForInvokables(@class);
            if (invokables != null && invokables.Any())
            {
                allInvokables.AddRange(invokables);
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
