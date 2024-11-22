// See https://aka.ms/new-console-template for more information


using GoLive.Generator.RazorPageRoute.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;

if (args.Length < 3)
{
    Console.WriteLine("Parameters required to run - <Settings File> <Project Path> <Namespace>");
    return;
}

var settingsFile = args[0];
var ProjectPath = args[1];
var @namespace = args[2];

var settings = PageRouteIncrementalExperimentalGenerator.LoadConfigFromFile(settingsFile, @namespace);
var routes = GetPageRoutes(ProjectPath);

List<PageRoute> GetPageRoutes(string projectPath)
{
    var debugPath = getHighestFolderVersion(Path.Combine(projectPath, "bin", "Debug"));;
    var objDebugPath = getHighestFolderVersion(Path.Combine(projectPath, "obj", "Debug"));
    var refIntPath = Path.Combine(objDebugPath, "refInt");
    var dllFile = Directory.GetFiles(refIntPath, "*.dll").FirstOrDefault();

    if (dllFile == null)
    {
        throw new FileNotFoundException("No DLL file found in refInt directory.");
    }

    //var defaultNetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "shared", "Microsoft.AspNetCore.App", "9.0.0");
    var assemblyResolver = new DefaultAssemblyResolver();
    assemblyResolver.AddSearchDirectory(refIntPath);
    //assemblyResolver.AddSearchDirectory(defaultNetPath);
    assemblyResolver.AddSearchDirectory(debugPath);
    var assembly = AssemblyDefinition.ReadAssembly(dllFile, new ReaderParameters { AssemblyResolver = assemblyResolver });
    
    var types = assembly.MainModule.Types
        .Where(t => t.BaseType != null && t is { IsClass: true, IsAbstract: false } && IsSubclassOf(t, componentBaseTypeName))
        .ToList();
    var routesList = new List<string>();
    
    foreach (var type in types)
    {
        var routeAttributes = type.CustomAttributes
            .Where(attr => attr.AttributeType.FullName == "Microsoft.AspNetCore.Components.RouteAttribute")
            .Select(attr => attr.ConstructorArguments.FirstOrDefault().Value?.ToString())
            .Where(route => !string.IsNullOrEmpty(route))
            .ToList();
    
        routesList.AddRange(routeAttributes);
    }
    
    foreach (var se in routesList)
    {
        Console.WriteLine(se);
    }
    
    bool IsSubclassOf(TypeDefinition type, string baseTypeName)
    {
        while (type != null && type.FullName != "System.Object")
        {
            if (type.FullName == baseTypeName)
            {
                return true;
            }
            type = type.BaseType?.Resolve();
        }
        return false;
    }

    throw new NotImplementedException();
}

PageRouteIncrementalExperimentalGenerator.GenerateOutput(default, settings, routes);

string? getHighestFolderVersion(string inputFolder, string searchPattern = "net*")
{
    var versionFolders = Directory.GetDirectories(inputFolder, searchPattern)
        .OrderByDescending(v => Version.Parse(Path.GetFileName(v)[3..]))
        .ToList();

    if (versionFolders.Count == 0)
    {
        throw new DirectoryNotFoundException("No version folders found in debug directory.");
    }

    var highestVersionFolder = versionFolders.First();

    return highestVersionFolder;
}

