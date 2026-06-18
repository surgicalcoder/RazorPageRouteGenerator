using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GoLive.Generator.RazorPageRoute.Generator;

public static class RouteExtractor
{
    public static List<string> ExtractRoutes(ClassDeclarationSyntax cls)
    {
        return cls.AttributeLists
            .SelectMany(al => al.Attributes)
            .Where(attr => IsRouteAttribute(attr))
            .SelectMany(attr => attr.ArgumentList?.Arguments
                .Where(a => a.Expression is LiteralExpressionSyntax les && les.IsKind(SyntaxKind.StringLiteralExpression))
                .Select(a => ((LiteralExpressionSyntax)a.Expression).Token.ValueText)
                ?? [])
            .ToList();
    }

    public static List<PageRouteQuerystringParameter> ExtractQueryParams(ClassDeclarationSyntax cls)
    {
        var result = new List<PageRouteQuerystringParameter>();

        var properties = cls.DescendantNodes().OfType<PropertyDeclarationSyntax>();
        foreach (var prop in properties)
        {
            if (HasSupplyParameterFromQueryAttribute(prop.AttributeLists))
            {
                result.Add(new PageRouteQuerystringParameter(prop.Identifier.Text, prop.Type.ToString()));
            }
        }

        var fields = cls.DescendantNodes().OfType<FieldDeclarationSyntax>();
        foreach (var field in fields)
        {
            if (HasSupplyParameterFromQueryAttribute(field.AttributeLists))
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    result.Add(new PageRouteQuerystringParameter(variable.Identifier.Text, field.Declaration.Type.ToString()));
                }
            }
        }

        return result;
    }

    public static PageRouteAuth ExtractAuth(ClassDeclarationSyntax cls, Settings settings, Func<string, List<string>, string> resolveConstValue)
    {
        var auth = new PageRouteAuth();
        var compilationUsings = GetCompilationUsings(cls);

        foreach (var attrList in cls.AttributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                if (IsAuthorizeAttribute(attr))
                {
                    auth.RequiresAuthentication = true;
                    ExtractAuthorizeArguments(attr, auth, resolveConstValue, compilationUsings);
                }
                else
                {
                    TryExtractCustomAuth(attr, settings, auth, resolveConstValue, compilationUsings);
                }
            }
        }

        return auth;
    }

    public static List<Invokable> ExtractInvokables(ClassDeclarationSyntax cls)
    {
        var result = new List<Invokable>();

        foreach (var method in cls.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            foreach (var attrList in method.AttributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    if (IsJsInvokableAttribute(attr)
                        && attr.ArgumentList?.Arguments.Count > 0
                        && attr.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax les
                        && les.IsKind(SyntaxKind.StringLiteralExpression)
                        && !string.IsNullOrEmpty(les.Token.ValueText))
                    {
                        var identifier = les.Token.ValueText;
                        result.Add(new Invokable($"{cls.Identifier.Text}.{method.Identifier.Text}", identifier));
                    }
                }
            }
        }

        return result;
    }

    private static bool IsRouteAttribute(AttributeSyntax attr)
    {
        var name = attr.Name.ToString();
        return name is "Route" or "RouteAttribute"
            || name == "Microsoft.AspNetCore.Components.RouteAttribute"
            || name.EndsWith(".RouteAttribute");
    }

    private static bool IsAuthorizeAttribute(AttributeSyntax attr)
    {
        var name = attr.Name.ToString();
        return name is "Authorize" or "AuthorizeAttribute"
            || name == "Microsoft.AspNetCore.Authorization.AuthorizeAttribute"
            || name.EndsWith(".AuthorizeAttribute");
    }

    private static bool IsJsInvokableAttribute(AttributeSyntax attr)
    {
        var name = attr.Name.ToString();
        return name is "JSInvokable" or "JSInvokableAttribute"
            || name == "Microsoft.JSInterop.JSInvokableAttribute"
            || name.EndsWith(".JSInvokableAttribute");
    }

    private static bool HasSupplyParameterFromQueryAttribute(SyntaxList<AttributeListSyntax> attrLists)
    {
        return attrLists.SelectMany(al => al.Attributes)
            .Any(attr =>
            {
                var name = attr.Name.ToString();
                return name is "SupplyParameterFromQuery" or "SupplyParameterFromQueryAttribute"
                    || name.EndsWith(".SupplyParameterFromQuery")
                    || name.EndsWith(".SupplyParameterFromQueryAttribute");
            });
    }

    private static void ExtractAuthorizeArguments(AttributeSyntax attr, PageRouteAuth auth, Func<string, List<string>, string> resolveConstValue, List<string> compilationUsings)
    {
        if (attr.ArgumentList == null) return;

        foreach (var arg in attr.ArgumentList.Arguments)
        {
            if (arg.NameEquals != null)
            {
                var argName = arg.NameEquals.Name.Identifier.Text;
                var value = GetAttributeArgumentValue(arg, resolveConstValue, compilationUsings);

                switch (argName)
                {
                    case "Roles":
                        auth.Roles = value.Split(',').Select(r => r.Trim()).ToList();
                        break;
                    case "Policy":
                        auth.Policies = value.Split(',').Select(p => p.Trim()).ToList();
                        break;
                    case "AuthenticationSchemes":
                        auth.AuthenticationSchemes = value.Split(',').Select(s => s.Trim()).ToList();
                        break;
                }
            }
            else
            {
                var value = GetAttributeArgumentValue(arg, resolveConstValue, compilationUsings);
                if (!string.IsNullOrEmpty(value))
                {
                    auth.Roles = value.Split(',').Select(r => r.Trim()).ToList();
                }
            }
        }
    }

    private static void TryExtractCustomAuth(AttributeSyntax attr, Settings settings, PageRouteAuth auth, Func<string, List<string>, string> resolveConstValue, List<string> compilationUsings)
    {
        if (settings.Auth == null) return;

        var attrName = attr.Name.ToString();
        foreach (var customAuth in settings.Auth)
        {
            if (customAuth.Attribute == null) continue;
            if (!customAuth.Attribute.Any(a => a == attrName || a.EndsWith("." + attrName))) continue;

            auth.RequiresAuthentication = true;

            var ctorArgs = new Dictionary<string, string>();
            var namedArgs = new Dictionary<string, string>();

            if (attr.ArgumentList != null)
            {
                for (int i = 0; i < attr.ArgumentList.Arguments.Count; i++)
                {
                    var arg = attr.ArgumentList.Arguments[i];
                    if (arg.NameEquals != null)
                    {
                        namedArgs[arg.NameEquals.Name.Identifier.Text] = GetAttributeArgumentValue(arg, resolveConstValue, compilationUsings);
                    }
                    else
                    {
                        ctorArgs[$"arg{i}"] = GetAttributeArgumentValue(arg, resolveConstValue, compilationUsings);
                    }
                }
            }

            auth.CustomAuth ??= new List<PageRouteAuthCustomAuth>();
            auth.CustomAuth.Add(new PageRouteAuthCustomAuth(attrName, ctorArgs, namedArgs));
        }
    }

    private static string GetAttributeArgumentValue(AttributeArgumentSyntax arg, Func<string, List<string>, string> resolveConstValue, List<string> compilationUsings)
    {
        if (arg.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        var exprStr = arg.Expression.ToString();

        if (!string.IsNullOrEmpty(exprStr))
        {
            var resolved = resolveConstValue(exprStr, compilationUsings);
            if (!string.IsNullOrEmpty(resolved))
            {
                return resolved;
            }
        }

        return exprStr;
    }

    private static List<string> GetCompilationUsings(ClassDeclarationSyntax cls)
    {
        var root = cls.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault();
        if (root == null) return [];

        return root.Usings
            .Select(u => u.Name?.ToString())
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList();
    }
}
