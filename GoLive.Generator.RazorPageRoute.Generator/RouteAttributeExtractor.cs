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
                var queryString = new List<PageRouteQuerystringParameter>();

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

                var properties = classDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>();
                foreach (var property in properties)
                {
                    var hasQueryAttribute = property.AttributeLists
                        .SelectMany(attrList => attrList.Attributes)
                        .Any(attr => attr.Name.ToString().Contains("SupplyParameterFromQueryAttribute") || attr.Name.ToString().Contains("SupplyParameterFromQuery"));
                
                    if (hasQueryAttribute)
                    {
                        var propertyName = property.Identifier.Text;
                        var propertyType = property.Type.ToString();
                        queryString.Add(new PageRouteQuerystringParameter(propertyName, propertyType));
                    }
                }
                
                if (routes.Any())
                {
                    extractedRoutes.Add(new ExtractedRoutes { ClassName = className, Routes = routes, QueryString = queryString });
                }
            }
        }

        return extractedRoutes;
    }
}