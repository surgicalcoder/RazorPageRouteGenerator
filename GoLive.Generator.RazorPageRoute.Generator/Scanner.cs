using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GoLive.Generator.RazorPageRoute.Generator
{
    public static class Scanner
    {
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

            foreach (var attributeData in attributes)
            {
                var route = attributeData?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? string.Empty;

                yield return new PageRoute(classSymbol.Name, route);
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
        }
    }
}