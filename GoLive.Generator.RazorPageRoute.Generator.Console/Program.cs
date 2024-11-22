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

var settings = PageRouteIncrementalExperimentalGenerator.LoadConfigFromFile(settingsFile, @namespace);
var routes = GetPageRoutes(projectPath);

List<PageRoute> GetPageRoutes(string projectPath)
{
    var dllFile = Scanner.GetDllPathFromProject(projectPath, out var assemblyResolver);

    using var assembly = AssemblyDefinition.ReadAssembly(dllFile, new ReaderParameters { AssemblyResolver = assemblyResolver });

    return Scanner.ScanForPageRoutesIncremental(assembly).DistinctBy(route => route.Route).ToList();
}

PageRouteIncrementalExperimentalGenerator.GenerateOutput(default, settings, routes);