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

    public static IEnumerable<Invokable> ScanForInvokables(ClassInfo input)
    {
        var types = new List<ClassInfo>();
        if (input.BaseTypes != null && input.BaseTypes.Contains(componentBaseTypeName) &&
            (input.Modifiers == null || !input.Modifiers.Contains("abstract")))
        {
            types.Add(input);
        }

        foreach (var type in types)
        {
            foreach (var method in type.Methods)
            {
                var jsInvokableAttr = method.Attributes.FirstOrDefault(attr =>
                    attr.Name == jsInvokableAttribute &&
                    attr.Arguments.Count > 0 &&
                    !string.IsNullOrEmpty(attr.Arguments[0]));

                if (jsInvokableAttr != null)
                {
                    string methodName = method.Name;
                    string identifier = jsInvokableAttr.Arguments[0];
                    yield return new Invokable($"{type.Name}.{methodName}", identifier);
                }
            }
        }
    }

    public static IEnumerable<PageRoute> ScanForPageRoutes(ClassInfo input, Settings settings)
    {
        var routes = input.Attributes
            .Where(attr => attr.Name == "Microsoft.AspNetCore.Components.RouteAttribute")
            .SelectMany(attr => attr.Arguments)
            .Where(route => !string.IsNullOrEmpty(route))
            .ToList();

        var queryStringParams =
            input.Fields.Where(e => e.Attributes.Any(f => f.Name == "SupplyParameterFromQuery"))
                .Select(e => new PageRouteQuerystringParameter(e.Name, e.Type))
                .ToList();

        var pageAuth = getPageAuth(input, settings);
        var invokables = ScanForInvokables(input).ToList();

        foreach (var route in routes)
        {
            yield return new PageRoute(input.Name, route, queryStringParams, pageAuth, invokables);
        }
    }


    private static PageRouteAuth getPageAuth(ClassInfo input, Settings settings)
    {
        var retr = new PageRouteAuth();

        // Built-in AuthorizeAttribute
        var authorizeAttributes = input.Attributes
            .Where(attr => attr.Name is "Microsoft.AspNetCore.Authorization.AuthorizeAttribute" or "AuthorizeAttribute" or "Authorize")
            .ToList();

        foreach (var attr in authorizeAttributes)
        {
            retr.RequiresAuthentication = true;

            foreach (var arg in attr.Arguments)
            {
                if (!string.IsNullOrEmpty(arg))
                {
                    retr.Roles = arg.Split(',').Select(role => role.Trim()).ToList();
                }
            }

            // Handle named properties
            if (attr.NamedArguments.TryGetValue("Policy", out var policy))
            {
                retr.Policies = policy.Split(',').Select(p => p.Trim()).ToList();
            }
            if (attr.NamedArguments.TryGetValue("AuthenticationSchemes", out var schemes))
            {
                retr.AuthenticationSchemes = schemes.Split(',').Select(s => s.Trim()).ToList();
            }
        }

        // Named properties (Policy, AuthenticationSchemes) are not available in AttributeInfo, so skip for ClassInfo

        // Custom attributes from settings.Auth
        if (settings.Auth != null)
        {
            foreach (var customAuth in settings.Auth)
            {
                var customAttributes = input.Attributes
                    .Where(attr => customAuth.Attribute != null && customAuth.Attribute.Contains(attr.Name))
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