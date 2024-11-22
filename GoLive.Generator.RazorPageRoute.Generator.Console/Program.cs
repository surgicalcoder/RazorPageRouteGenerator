// See https://aka.ms/new-console-template for more information

using System.Reflection;
using GoLive.Generator.RazorPageRoute.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

if (args.Length < 3)
{
    Console.WriteLine("Parameters required to run - <Settings File> <Obj Path> <Namespace>");
    return;
}

var settingsFile = args[0];
var objPath = args[1];
var @namespace = args[2];

var settings = PageRouteIncrementalExperimentalGenerator.LoadConfigFromFile(settingsFile, @namespace);
var routes = GetPageRoutes(objPath);

List<PageRoute> GetPageRoutes(string objPath)
{
    var debugPath = Path.Combine(objPath, "debug");
    var versionFolders = Directory.GetDirectories(debugPath, "net*")
                                  .Select(Path.GetFileName)
                                  .OrderByDescending(v => Version.Parse(v[3..]))
                                  .ToList();

    if (versionFolders.Count == 0)
    {
        throw new DirectoryNotFoundException("No version folders found in debug directory.");
    }

    var highestVersionFolder = versionFolders.First();
    var refIntPath = Path.Combine(debugPath, highestVersionFolder, "refInt");
    var dllFile = Directory.GetFiles(refIntPath, "*.dll").FirstOrDefault();

    if (dllFile == null)
    {
        throw new FileNotFoundException("No DLL file found in refInt directory.");
    }


    var assembly = Assembly.LoadFile(dllFile);
    var componentBaseTypeName = "Microsoft.AspNetCore.Components.ComponentBase";
    var types = assembly.GetTypes()
        .Where(t => t.BaseType != null && t is { IsClass: true, IsAbstract: false } && IsSubclassOf(t, componentBaseTypeName))
        .ToList();
    var routesList = new List<string>();
    
    foreach (var type in types)
    {
        var routeAttributes = type.GetCustomAttributes()
            .Where(attr => attr.GetType().FullName == "Microsoft.AspNetCore.Components.RouteAttribute")
            .Select(attr => attr.GetType().GetProperty("Template")?.GetValue(attr)?.ToString())
            .Where(route => !string.IsNullOrEmpty(route))
            .ToList();
    
        routesList.AddRange(routeAttributes);
    }
    
    foreach (var se in routesList)
    {
        Console.WriteLine(se);
    }
    
    
    bool IsSubclassOf(Type type, string baseTypeName)
    {
        while (type != null && type.FullName != "System.Object")
        {
            if (type.FullName == baseTypeName)
            {
                return true;
            }
            type = type.BaseType;
        }
        return false;
    }

    throw new NotImplementedException();
}

PageRouteIncrementalExperimentalGenerator.GenerateOutput(default, settings, routes);

