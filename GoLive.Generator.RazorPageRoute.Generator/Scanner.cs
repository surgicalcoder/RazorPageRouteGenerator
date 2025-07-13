using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoLive.Generator.RazorPageRoute.Generator.CodeReader;
using Mono.Cecil;

namespace GoLive.Generator.RazorPageRoute.Generator;

public static class Scanner
{
    private static readonly string componentBaseTypeName = "Microsoft.AspNetCore.Components.ComponentBase";
    public const string jsInvokableAttribute = "Microsoft.JSInterop.JSInvokableAttribute";

    public static IEnumerable<(string MethodName, string InvokableName)> ScanForInvokables(AssemblyDefinition input)
    {
        var types = input.MainModule.Types
            .Where(t => t.BaseType != null && t is { IsClass: true, IsAbstract: false } &&
                        IsSubclassOf(t, componentBaseTypeName))
            .ToList();

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
    
    public static string GetDllPathFromProject(string projectPath, out DefaultAssemblyResolver assemblyResolver, string[] additionalSearchDirectories = null)
    {
        var debugPath = getHighestFolderVersion(Path.Combine(projectPath, "bin", "Debug"));
        
        var objDebugPath = getHighestFolderVersion(Path.Combine(projectPath, "obj", "Debug"));
        var refIntPath = Path.Combine(objDebugPath, "refInt");

        string dllFile = null;
        int retries = 3;
        while (retries > 0)
        {
            dllFile = Directory.GetFiles(refIntPath, "*.dll").FirstOrDefault();
            if (dllFile != null)
            {
                break;
            }
            retries--;
            System.Threading.Thread.Sleep(150 * (int)Math.Pow(2, 3 - retries));
        }

        if (dllFile == null)
        {
            throw new FileNotFoundException($"No DLL file found in refInt directory (Looked in {refIntPath}).");
        }

        assemblyResolver = new DefaultAssemblyResolver();
        assemblyResolver.AddSearchDirectory(refIntPath);
        if (additionalSearchDirectories != null)
        {
            foreach (var searchDirectory in additionalSearchDirectories)
            {
                assemblyResolver.AddSearchDirectory(searchDirectory);
            }
        }
        //assemblyResolver.AddSearchDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "shared", "Microsoft.AspNetCore.App", "9.0.0"));
        assemblyResolver.AddSearchDirectory(debugPath);

        return dllFile;
    }

    public static IEnumerable<PageRoute> ScanForPageRoutes(AnalysisResult input, Settings settings)
    {

    }

    public static IEnumerable<PageRoute> ScanForPageRoutesIncremental(AssemblyDefinition input, Settings settings)
    {
        var types = input.MainModule.Types
            .Where(t => t.BaseType != null && t is { IsClass: true, IsAbstract: false } &&
                        IsSubclassOf(t, componentBaseTypeName))
            .ToList();

        foreach (var pageRoute in types.Select(definition => ToRoute(definition, settings)).SelectMany(res => res))
        {
            yield return pageRoute;
        }
    }

    private static IEnumerable<PageRoute> ToRoute(ClassInfo input, Settings settings)
    {
        var routes = input.Attributes
            .Where(attr => attr.Name == "Microsoft.AspNetCore.Components.RouteAttribute")
            .SelectMany(attr => attr.Arguments)
            .Where(route => !string.IsNullOrEmpty(route))
            .ToList();

        var queryStringParams =
            input.Fields.Where(e => e.Attributes.Any(f => f.Name == "SupplyParameterFromQuery"))
                .Select(e => new PageRouteQuerystringParameter(e.Name, e.Type));

        var pageAuth = getPageAuth(input, settings);

        foreach (var route in routes)
        {
            yield return new PageRoute(input.Name, route, queryStringParams.ToList(), pageRouteAuth);
        }
    }




    private static IEnumerable<PageRoute> ToRoute(TypeDefinition input, Settings settings)
    {
        var classAttributes = GetAttributes(input.CustomAttributes);

        var routes = classAttributes
            .Where(attr => attr.AttributeType.FullName == "Microsoft.AspNetCore.Components.RouteAttribute")
            .Select(attr => attr.ConstructorArguments.FirstOrDefault().Value?.ToString())
            .Where(route => !string.IsNullOrEmpty(route))
            .ToList();

        var queryStringParams = input.Properties;

        var querystringParameters = queryStringParams
            .Where(p => p.CustomAttributes.Any(attr =>
                attr.AttributeType.FullName == "Microsoft.AspNetCore.Components.SupplyParameterFromQueryAttribute"))
            .Select(p => new PageRouteQuerystringParameter(p.Name, p.PropertyType.FullName))
            .ToList();

        var pageRouteAuth = getPageRouteAuth(input, settings);

        foreach (var route in routes)
        {
            yield return new PageRoute(input.Name, route, querystringParameters, pageRouteAuth);
        }
    }

    private static PageRouteAuth getPageAuth(ClassInfo input, Settings settings)
    {
        var retr = new PageRouteAuth();

        // Built-in AuthorizeAttribute
        var authorizeAttributes = input.Attributes
            .Where(attr => attr.Name == "Microsoft.AspNetCore.Authorization.AuthorizeAttribute")
            .ToList();

        foreach (var attr in authorizeAttributes)
        {
            retr.RequiresAuthentication = true;

            foreach (var arg in attr.Arguments)
            {
                if (!string.IsNullOrEmpty(arg))
                {
                    // Assume argument is a comma-separated string of roles
                    retr.Roles = arg.Split(',').Select(role => role.Trim()).ToList();
                }
            }
        }

        // Named properties (Policy, AuthenticationSchemes) are not available in AttributeInfo, so skip for ClassInfo

        // Custom attributes from settings.Auth
        if (settings.Auth != null)
        {
            foreach (var customAuth in settings.Auth)
            {
                var customAttributes = input.Attributes
                    .Where(attr => attr.Name == customAuth.Attribute)
                    .ToList();

                foreach (var attr in customAttributes)
                {
                    retr.RequiresAuthentication = true;

                    var ctorArgs = new Dictionary<string, string>();
                    var namedArgs = new Dictionary<string, string>();

                    // AttributeInfo only has Arguments, treat as constructor args
                    for (int i = 0; i < attr.Arguments.Count; i++)
                    {
                        ctorArgs[$"arg{i}"] = attr.Arguments[i];
                    }

                    // No named properties in AttributeInfo, so namedArgs stays empty
                    retr.CustomAuth ??= new List<PageRouteAuthCustomAuth>();
                    retr.CustomAuth.Add(new PageRouteAuthCustomAuth(attr.Name, ctorArgs, namedArgs));
                }
            }
        }

        return retr;
    }

    private static PageRouteAuth getPageRouteAuth(TypeDefinition input, Settings settings)
    {
        PageRouteAuth retr = new();

        // Check for built-in Authorize attributes
        var authorizeAttributes = input.CustomAttributes
            .Where(attr => attr.AttributeType.FullName == "Microsoft.AspNetCore.Authorization.AuthorizeAttribute")
            .ToList();
        
        foreach (var attr in authorizeAttributes)
        {
            retr.RequiresAuthentication = true;

            foreach (var arg in attr.ConstructorArguments)
            {
                if (arg.Type.FullName == "System.String")
                {
                    // Try to get the constant reference if available
                    var argConstant = arg.Value as FieldReference;

                    if (argConstant != null && argConstant.DeclaringType != null)
                    {
                        retr.Roles = new List<string> { $"{argConstant.DeclaringType.Name}.{argConstant.Name}" };
                    }
                    else
                    {
                        var argValue = arg.Value?.ToString();

                        if (!string.IsNullOrEmpty(argValue))
                        {
                            retr.Roles = argValue.Split(',').Select(role => role.Trim()).ToList();
                        }
                    }
                }
            }

            foreach (var namedArg in attr.Properties)
            {
                var roleArgument = namedArg.Argument.Value;
                var roleConstant = roleArgument as FieldReference;

                if (roleConstant != null && roleConstant.DeclaringType != null)
                {
                    retr.Roles = new List<string> { $"{roleConstant.DeclaringType.Name}.{roleConstant.Name}" };
                }
                else
                {
                    retr.Roles = roleArgument.ToString().Split(',').Select(role => role.Trim()).ToList();
                }

                if (namedArg.Name == "Policy")
                {
                    var policyArgument = namedArg.Argument.Value;
                    var policyConstant = policyArgument as FieldReference;

                    if (policyConstant != null && policyConstant.DeclaringType != null)
                    {
                        retr.Policies = new List<string> { $"{policyConstant.DeclaringType.Name}.{policyConstant.Name}" };
                    }
                    else
                    {
                        retr.Policies = policyArgument.ToString().Split(',').Select(policy => policy.Trim()).ToList();
                    }
                }

                if (namedArg.Name == "AuthenticationSchemes")
                {
                    var schemeArgument = namedArg.Argument.Value;
                    var schemeConstant = schemeArgument as FieldReference;

                    if (schemeConstant != null && schemeConstant.DeclaringType != null)
                    {
                        retr.AuthenticationSchemes = new List<string> { $"{schemeConstant.DeclaringType.Name}.{schemeConstant.Name}" };
                    }
                    else
                    {
                        retr.AuthenticationSchemes = schemeArgument.ToString().Split(',').Select(scheme => scheme.Trim()).ToList();
                    }
                }
                
            }
        }

        // Check for custom attributes from settings.Auth
        foreach (var customAuth in settings.Auth)
        {
            var customAttributes = input.CustomAttributes
                .Where(attr => attr.AttributeType.FullName == customAuth.Attribute)
                .ToList();

            foreach (var attr in customAttributes)
            {
                retr.RequiresAuthentication = true;

                var ctorArgs = new Dictionary<string, string>();
                var namedArgs = new Dictionary<string, string>();

                foreach (var arg in attr.ConstructorArguments)
                {
                    var constructor = attr.AttributeType.Resolve().Methods
                        .First(m => m.IsConstructor && m.Parameters.Count == attr.ConstructorArguments.Count);

                    var parameterName = constructor.Parameters[attr.ConstructorArguments.IndexOf(arg)].Name;
                    var parameterValue = arg.Value is CustomAttributeArgument[] array
                        ? string.Join(",", array.Select(a => a.Value?.ToString()))
                        : arg.Value?.ToString();

                    if (parameterValue != null)
                    {
                        ctorArgs[parameterName] = parameterValue;
                    }
                }


                foreach (var namedArg in attr.Properties)
                {
                    namedArgs[namedArg.Name] = namedArg.Argument.Value.ToString();
                }
                retr.CustomAuth ??= [];
                retr.CustomAuth.Add(new PageRouteAuthCustomAuth(attr.AttributeType.FullName, ctorArgs, namedArgs));
            }
        }

        return retr;
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

    public static string? getHighestFolderVersion(string inputFolder, string searchPattern = "net*")
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