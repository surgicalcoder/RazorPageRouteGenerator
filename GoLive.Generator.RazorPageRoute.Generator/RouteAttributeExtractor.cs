using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GoLive.Generator.RazorPageRoute.Generator;

public class RouteAttributeExtractor
{
    public class ExtractedRoutes
    {
        public string ClassName { get; set; }
        public List<string> Routes { get; set; }
        public List<PageRouteQuerystringParameter> QueryString { get; set; }
    }

    public static List<ExtractedRoutes> ExtractRoutes(string projectPath)
    {
        var extractedRoutes = new List<ExtractedRoutes>();
        var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var code = File.ReadAllText(file);
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDeclaration in classDeclarations)
            {
                var className = classDeclaration.Identifier.Text;
                var routes = new List<string>();

                var attributes = classDeclaration.DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .Where(attr => attr.Name.ToString().Contains("RouteAttribute"));

                foreach (var attribute in attributes)
                {
                    var argument = attribute.ArgumentList?.Arguments.FirstOrDefault();

                    if (argument != null)
                    {
                        var route = argument.ToString().Trim('"');
                        routes.Add(route);
                    }
                }

                if (routes.Any())
                {
                    extractedRoutes.Add(new ExtractedRoutes { ClassName = className, Routes = routes });
                }
            }
        }

        return extractedRoutes;
    }
}