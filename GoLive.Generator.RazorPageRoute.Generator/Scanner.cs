using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace GoLive.Generator.RazorPageRoute.Generator;

public static class Scanner
{
    private static readonly string componentBaseTypeName = "Microsoft.AspNetCore.Components.ComponentBase";
    public const string jsInvokableAttribute = "Microsoft.JSInterop.JSInvokableAttribute";
            
        public static IEnumerable<(string MethodName, string InvokableName)> ScanForInvokables(AssemblyDefinition input)
        {
            var baseClass = input.MainModule.Types.FirstOrDefault(t => t.FullName == componentBaseTypeName);

            if (baseClass == null)
            {
                yield break;
            }
            
            var types = input.MainModule.Types.Where(t => t.BaseType != null && t.IsClass && !t.IsAbstract);
            
            foreach (var type in types)
            {
                if (IsSubclassOf(type, componentBaseTypeName))
                {
                    foreach (var method in type.Methods)
                    {
                        // Check if the method has the attribute [JSInvokable(string)]
                        var jsInvokableAttr = method.CustomAttributes.FirstOrDefault(attr =>
                            attr.AttributeType.FullName == jsInvokableAttribute &&
                            attr.ConstructorArguments.Count > 0 &&
                            attr.ConstructorArguments[0].Value is string);
        
                        if (jsInvokableAttr != null)
                        {
                            // Extract method name
                            string methodName = method.Name;

                            // Extract identifier in the attribute
                            string identifier = jsInvokableAttr.ConstructorArguments[0].Value.ToString();

                            yield return ($"{type.FullName}.{method.Name}", identifier);
                        }
                    }
                }
            }
        }
        
        

    public static string GetDllPathFromProject(string projectPath, out DefaultAssemblyResolver assemblyResolver)
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
        assemblyResolver = new DefaultAssemblyResolver();
        assemblyResolver.AddSearchDirectory(refIntPath);
        //assemblyResolver.AddSearchDirectory(defaultNetPath);
        assemblyResolver.AddSearchDirectory(debugPath);

        return dllFile;
    }
    
    public static IEnumerable<PageRoute> ScanForPageRoutesIncremental(AssemblyDefinition input)
    {
        var types = input.MainModule.Types
            .Where(t => t.BaseType != null && t is { IsClass: true, IsAbstract: false } && IsSubclassOf(t, componentBaseTypeName))
            .ToList();

        foreach (var type in types)
        {
            var res = ToRoute(type);

            foreach (var pageRoute in res)
            {
                yield return pageRoute;
            }
        }
    }

    private static IEnumerable<PageRoute> ToRoute(TypeDefinition input)
    {
        var classAttributes = GetAttributes(input.CustomAttributes);

        var routes = classAttributes
            .Where(attr => attr.AttributeType.FullName == "Microsoft.AspNetCore.Components.RouteAttribute")
            .Select(attr => attr.ConstructorArguments.FirstOrDefault().Value?.ToString())
            .Where(route => !string.IsNullOrEmpty(route))
            .ToList();

        var queryStringParams = input.Properties;

        var querystringParameters = queryStringParams
            .Where(p => p.CustomAttributes.Any(attr => attr.AttributeType.FullName == "Microsoft.AspNetCore.Components.SupplyParameterFromQueryAttribute"))
            .Select(p => new PageRouteQuerystringParameter(p.Name, p.PropertyType.FullName))
            .ToList();

        foreach (var route in routes)
        {
            yield return new PageRoute(input.Name, route, querystringParameters);
        }
    }

    private static bool IsSubclassOf(TypeDefinition type, string baseTypeName)
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

    private static IEnumerable<CustomAttribute> GetAttributes(IEnumerable<CustomAttribute> customAttributes)
    {
        return customAttributes ?? [];
    }
    
    static string? getHighestFolderVersion(string inputFolder, string searchPattern = "net*")
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
    
    
}