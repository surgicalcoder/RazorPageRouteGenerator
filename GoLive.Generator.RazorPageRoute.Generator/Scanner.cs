using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace GoLive.Generator.RazorPageRoute.Generator;

public static class Scanner
{
    private static SymbolDisplayFormat symbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
    
    public static bool CanBeRazorPage(SyntaxNode node)
        => node is ClassDeclarationSyntax c
           && !c.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword))
           && c.BaseList?.Types.Count > 0;

    
    public static bool IsRoutableRazorComponent(INamedTypeSymbol classDeclaration) => InheritsFrom(classDeclaration, "Microsoft.AspNetCore.Components.ComponentBase");
    
    public static IEnumerable<PageRoute> ConvertToRoute(INamedTypeSymbol classSymbol)
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
    
    
    private static AttributeData? FindAttribute(ISymbol symbol, Func<INamedTypeSymbol, bool> selectAttribute)
        => symbol
            .GetAttributes()
            .LastOrDefault(a => a?.AttributeClass != null && selectAttribute(a.AttributeClass));
    
    private static IEnumerable<AttributeData> FindAttributes(ISymbol symbol, Func<INamedTypeSymbol, bool> selectAttribute) => symbol.GetAttributes().Where(a => a?.AttributeClass != null && selectAttribute(a.AttributeClass));

    
    private static bool InheritsFrom(INamedTypeSymbol classDeclaration, string qualifiedBaseTypeName)
    {
        var currentDeclared = classDeclaration;

        while (currentDeclared.BaseType != null)
        {
            var currentBaseType = currentDeclared.BaseType;
            if (string.Equals(currentBaseType.ToDisplayString(symbolDisplayFormat), qualifiedBaseTypeName, StringComparison.Ordinal))
            {
                return true;
            }

            currentDeclared = currentBaseType;
        }

        return false;
    }
}