using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GoLive.Generator.RazorPageRoute.Generator;

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
        var classAttributes = GetForAttributes(input.AttributeLists);
            
        var queryStringParams = input.Members.OfType<PropertyDeclarationSyntax>();

        var querystringParameters = queryStringParams.Select(e => (e, GetForAttributes(e.AttributeLists))).Where(f => f.Item2.Any())
            .Select(f => new PageRouteQuerystringParameter(f.e.Identifier.ToFullString().Trim(), f.e.Type.ToFullString().Trim())).ToList(); 
            
        foreach (var attributeData in classAttributes)
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
                retr.Name = attributeSyntaxAttribute.Name.NormalizeWhitespace().ToFullString().Trim();
                retr.Values = attributeSyntaxAttribute.ArgumentList?.Arguments.Select(r => (r.Expression as LiteralExpressionSyntax).Token.Value).ToList();
                yield return retr;
            }
        }
    }
}