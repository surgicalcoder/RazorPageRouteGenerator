using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GoLive.Generator.RazorPageRoute.Generator
{
    public static class Scanner
    {
        public static IEnumerable<PageRoute> ScanForPageRoutesIncremental(ClassDeclarationSyntax input)
        {
            var res = ToRoute(input);

            foreach (var pageRoute in res)
            {
                yield return pageRoute;
            }
        }
        
        private static IEnumerable<PageRoute> ToRoute(ClassDeclarationSyntax input)
        {
            var classAttibutes = GetForAttributes(input.AttributeLists);
            //var attributes = FindAttributes(input, "Microsoft.AspNetCore.Components.RouteAttribute");

            var queryStringParams = input.Members.OfType<PropertyDeclarationSyntax>();
            /*var querystringParameters = (from qsParam in queryStringParams let qsAttr = 
                FindAttribute(qsParam, a => a.ToString() == "Microsoft.AspNetCore.Components.SupplyParameterFromQueryAttribute") 
                where qsAttr != null select new PageRouteQuerystringParameter(qsAttr?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? qsParam.Name, qsParam.Type)).ToList();*/
            //queryStringParams.Where(r=>r.AttributeLists.Where(e=>e.Attributes. ))
            //var querystringParameters = from qsParam in queryStringParams let asAttr = qsParam.AttributeLists.

            var querystringParameters = queryStringParams.Select(e => (e, GetForAttributes(e.AttributeLists))).Where(f => f.Item2.Any())
                .Select(f => new PageRouteQuerystringParameter(f.e.Identifier.ToFullString(), f.e.Type.ToFullString())).ToList(); // f.Where(e => e.Name == "SupplyParameterFromQueryAttribute"));
                //.SelectMany(f =>  f.Where(e => e.Name == "SupplyParameterFromQueryAttribute"));

            /*var querystringParameters = from qsParam in queryStringParams
                let qsAttr = GetForAttributes(qsParam.AttributeLists)
                where qsAttr != null && qsAttr.Any() && qsAttr.Where(r => r.Name == "Microsoft.AspNetCore.Components.SupplyParameterFromQueryAttribute" || r.Name == "SupplyParameterFromQueryAttribute");*/

            foreach (var attributeData in classAttibutes)
            {
                var route = attributeData?.Values.FirstOrDefault().ToString() ?? string.Empty;

                yield return new PageRoute(input.Identifier.Text, route, querystringParameters);
            }
        }
        
        
        private static IEnumerable<AttributeContainer> GetForAttributes(SyntaxList<AttributeListSyntax> input)
        {
            foreach (var attributeSyntax in input)
            {
                foreach (var attributeSyntaxAttribute in attributeSyntax.Attributes)
                {
                    AttributeContainer retr = new();
                    retr.Name = attributeSyntaxAttribute.Name.NormalizeWhitespace().ToFullString();
                    retr.Values = attributeSyntaxAttribute.ArgumentList.Arguments.Select(r => (r.Expression as LiteralExpressionSyntax).Token.Value).ToList();
                    yield return retr;
                }
            }
        }
        public class AttributeContainer
        {
            public string Name { get; set; }
            public List<object?> Values { get; set; } = new();
        }
        
        
        
        /*private static IEnumerable<AttributeSyntax> FindAttributes(TypeDeclarationSyntax syntax, string attributeSelector)
        {
            return syntax.AttributeLists.SelectMany(a => a.Attributes.Where(f => f.Name.GetText().ToString() == $"global::{attributeSelector}"));
        }


        public static IEnumerable<PageRoute> ScanForPageRoutes(SemanticModel semantic)
        {
            var baseClass = semantic.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.ComponentBase");

            if (baseClass == null)
            {
                yield break;
            }

            var allNodes = semantic.SyntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var node in allNodes)
            {
                if (semantic.GetDeclaredSymbol(node) is INamedTypeSymbol classSymbol && InheritsFrom(classSymbol, baseClass))
                {
                    var res = ToRoute(classSymbol);

                    foreach (var pageRoute in res)
                    {
                        yield return pageRoute;
                    }
                }
            }
        }

        private static IEnumerable<PageRoute> ToRoute(INamedTypeSymbol classSymbol)
        {
            var attributes = FindAttributes(classSymbol, a => a.ToString() == "Microsoft.AspNetCore.Components.RouteAttribute");

            var queryStringParams = classSymbol.GetMembers().OfType<IPropertySymbol>();
            var querystringParameters = (from qsParam in queryStringParams let qsAttr = FindAttribute(qsParam, a => a.ToString() == "Microsoft.AspNetCore.Components.SupplyParameterFromQueryAttribute") where qsAttr != null select new PageRouteQuerystringParameter(qsAttr?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? qsParam.Name, qsParam.Type)).ToList();

            foreach (var attributeData in attributes)
            {
                var route = attributeData?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? string.Empty;

                yield return new PageRoute(classSymbol.Name, route, querystringParameters);
            }
        }

        private static IEnumerable<AttributeData> FindAttributes(ISymbol symbol, Func<INamedTypeSymbol, bool> selectAttribute) => symbol.GetAttributes().Where(a => a?.AttributeClass != null && selectAttribute(a.AttributeClass));

        private static AttributeData? FindAttribute(ISymbol symbol, Func<INamedTypeSymbol, bool> selectAttribute) => symbol.GetAttributes().LastOrDefault(a => a?.AttributeClass != null && selectAttribute(a.AttributeClass));

        private static bool InheritsFrom(INamedTypeSymbol classDeclaration, INamedTypeSymbol targetBaseType)
        {
            var currentDeclared = classDeclaration;

            while (currentDeclared.BaseType != null)
            {
                var currentBaseType = currentDeclared.BaseType;

                if (currentBaseType.Equals(targetBaseType, SymbolEqualityComparer.Default))
                {
                    return true;
                }

                currentDeclared = currentDeclared.BaseType;
            }

            return false;
        }*/
    }
}