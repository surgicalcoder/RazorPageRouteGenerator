using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GoLive.Generator.RazorPageRoute.Generator;

public static class FileParseScanner
{
    public static IEnumerable<PageRoute> ParseCSharpFiles(string ProjectPath)
    {
        var rootPath = Path.Combine(ProjectPath, "obj//Debug//net9.0//RazorDeclaration//Pages");
        
        var files = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);
        
        var attributes = RouteAttributeExtractor.ExtractRoutes(rootPath);

        if (attributes.Any())
        {
            foreach (var extractedRoute in attributes)
            {
                foreach (var extractedRouteRoute in extractedRoute.Routes)
                {
                    yield return new PageRoute(extractedRoute.ClassName, extractedRouteRoute, extractedRoute.QueryString);
                }
            }
        }
    }
}