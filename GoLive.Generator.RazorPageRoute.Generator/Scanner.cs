using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace GoLive.Generator.RazorPageRoute.Generator;

public static class Scanner
{
    private static readonly string componentBaseTypeName = "Microsoft.AspNetCore.Components.ComponentBase";

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
}