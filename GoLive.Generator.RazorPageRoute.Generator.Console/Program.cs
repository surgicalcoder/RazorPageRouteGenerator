// See https://aka.ms/new-console-template for more information
using GoLive.Generator.RazorPageRoute.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;
using System.Linq;
using System.Linq.Expressions;


if (args.Length < 3)
{
    Console.WriteLine("Parameters required to run - <Settings File> <Project Path> <Namespace>");
    return;
}

var settingsFile = args[0];
var projectPath = args[1];
var @namespace = args[2];

Console.WriteLine($"Running RPRG for settings: {settingsFile} in project path of {projectPath} with a namespace of {@namespace}");

var settings = PageRouteIncrementalExperimentalGenerator.LoadConfigFromFile(settingsFile, @namespace);

var net9SearchDirectory = GetHighestInstalledNetVersion();
//var net9SearchDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "shared", "Microsoft.AspNetCore.App", "9.0.0");
var routes = GetPageRoutes(projectPath).ToList();

Console.WriteLine($"Running RPRG for net9 search directory: {net9SearchDirectory}");

IEnumerable<PageRoute> GetPageRoutes(string projectPath)
{
    try
    {
        string dllFile;
        dllFile = Scanner.GetDllPathFromProject(projectPath, out var assemblyResolver, [net9SearchDirectory]);
        using var assembly = AssemblyDefinition.ReadAssembly(dllFile, new ReaderParameters { AssemblyResolver = assemblyResolver });

        return Scanner.ScanForPageRoutesIncremental(assembly, settings).DistinctBy(route => route.Route).ToList();
    }
    catch (FileNotFoundException)
    {
        Console.WriteLine("refInt directory not found, scanning for C# files instead.");
        return FileParseScanner.ParseCSharpFiles(projectPath);
    }
}

IEnumerable<(string MethodName, string InvokableName)> GetInvokeables(string projectPath)
{
    try
    {
        var dllFile = Scanner.GetDllPathFromProject(projectPath, out var assemblyResolver, [net9SearchDirectory]);

        using var assembly = AssemblyDefinition.ReadAssembly(dllFile, new ReaderParameters { AssemblyResolver = assemblyResolver });

        return Scanner.ScanForInvokables(assembly);
    }
    catch (FileNotFoundException e)
    {
        Console.WriteLine("refInt directory not found, source scanning not currently supported for invokables.");

        return new List<(string MethodName, string InvokableName)>();;
    }
}

PageRouteIncrementalExperimentalGenerator.GenerateOutput(default, settings, routes);
PageRouteIncrementalExperimentalGenerator.GenerateJSInvokable(settings, GetInvokeables(projectPath).ToList());

return;


string GetHighestInstalledNetVersion()
{
    string sharedPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        "dotnet", "shared", "Microsoft.AspNetCore.App");

    if (Directory.Exists(sharedPath))
    {
        var versions = Directory.GetDirectories(sharedPath)
                                .Select(Path.GetFileName)
                                .Where(v => Version.TryParse(v, out _))
                                .Select(v => Version.Parse(v))
                                .OrderByDescending(v => v)
                                .ToList();

        if (versions.Count != 0)
        {
            var highestVersion = versions.First();
            var fullPath = Path.Combine(sharedPath, highestVersion.ToString());
            //Console.WriteLine($"Highest installed ASP.NET Core version path: {fullPath}");
            return fullPath;
        }
    }
    
    throw new DirectoryNotFoundException("ASP.NET Core shared directory not found.");
}