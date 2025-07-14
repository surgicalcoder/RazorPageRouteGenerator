using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GoLive.Generator.RazorPageRoute.Generator.CodeReader;

public static class SourceCodeAnalyzer
{
    public static AnalysisResult AnalyzeSourceFile(string filePath, Func<string, List<string>, string> ResolveConstValueFunc)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Source file not found: {filePath}");
        }

        var sourceCode = File.ReadAllText(filePath);
        return AnalyzeSourceCode(sourceCode, ResolveConstValueFunc);
    }

    public static AnalysisResult AnalyzeSourceCode(string sourceCode, Func<string, List<string>, string> ResolveConstValueFunc)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = syntaxTree.GetCompilationUnitRoot();

        var result = new AnalysisResult();

        var namespaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();
        foreach (var ns in namespaces)
        {
            result.Namespaces.Add(new NamespaceInfo
            {
                Name = ns.Name.ToString(),
                FullName = ns.Name.ToString()
            });
        }

        var usingDirectives = root.Usings.Select(u => u.Name?.ToString()).ToList();

        // Also collect using directives inside namespace declarations
        var namespaceUsings = root.DescendantNodes()
            .OfType<NamespaceDeclarationSyntax>()
            .SelectMany(ns => ns.Usings.Select(u => u.Name?.ToString()))
            .ToList();

        // Also collect using directives inside file-scoped namespace declarations (.NET 6+)
        var fileScopedNamespaceUsings = root.DescendantNodes()
            .OfType<FileScopedNamespaceDeclarationSyntax>()
            .SelectMany(ns => ns.Usings.Select(u => u.Name?.ToString()))
            .ToList();

        // Combine all using directives, remove nulls and duplicates
        var allUsings = usingDirectives
            .Concat(namespaceUsings)
            .Concat(fileScopedNamespaceUsings)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct()
            .ToList();

        result.ReferencedNamespaces.AddRange(allUsings);
        result.ReferencedNamespaces.AddRange(result.Namespaces.Select(r=>r.FullName));

        // Remove "global::" prefix and duplicates (case-insensitive)
        result.ReferencedNamespaces = result.ReferencedNamespaces
            .Select(ns => ns.StartsWith("global::", StringComparison.Ordinal) ? ns["global::".Length..] : ns)
            .Where(ns => !string.IsNullOrWhiteSpace(ns))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();


        // Extract file-scoped namespaces (.NET 6+)
        var fileScopedNamespaces = root.DescendantNodes().OfType<FileScopedNamespaceDeclarationSyntax>();
        foreach (var ns in fileScopedNamespaces)
        {
            result.Namespaces.Add(new NamespaceInfo
            {
                Name = ns.Name.ToString(),
                FullName = ns.Name.ToString()
            });
        }

        // Extract classes
        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
        foreach (var cls in classes)
        {
            var classInfo = new ClassInfo
            {
                Name = cls.Identifier.ValueText,
                Modifiers = cls.Modifiers.ToString(),
                BaseTypes = cls.BaseList?.Types.Select(t => t.Type.ToString()).ToList() ?? new List<string>(),
                Attributes = ExtractAttributes(cls.AttributeLists, ResolveConstValueFunc, result.ReferencedNamespaces)
            };

            // Extract properties
            var properties = cls.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var prop in properties)
            {
                classInfo.Properties.Add(new PropertyInfo
                {
                    Name = prop.Identifier.ValueText,
                    Type = prop.Type.ToString(),
                    Modifiers = prop.Modifiers.ToString(),
                    Attributes = ExtractAttributes(prop.AttributeLists, ResolveConstValueFunc, result.ReferencedNamespaces),
                    HasGetter = prop.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? false,
                    HasSetter = prop.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false
                });
            }

            // Extract fields
            var fields = cls.DescendantNodes().OfType<FieldDeclarationSyntax>();
            foreach (var field in fields)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    classInfo.Fields.Add(new FieldInfo
                    {
                        Name = variable.Identifier.ValueText,
                        Type = field.Declaration.Type.ToString(),
                        Modifiers = field.Modifiers.ToString(),
                        Attributes = ExtractAttributes(field.AttributeLists, ResolveConstValueFunc, result.ReferencedNamespaces),
                        HasInitializer = variable.Initializer != null
                    });
                }
            }

            // Extract methods
            var methods = cls.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                classInfo.Methods.Add(new MethodInfo
                {
                    Name = method.Identifier.ValueText,
                    ReturnType = method.ReturnType.ToString(),
                    Modifiers = method.Modifiers.ToString(),
                    Attributes = ExtractAttributes(method.AttributeLists, ResolveConstValueFunc, result.ReferencedNamespaces),
                    Parameters = method.ParameterList.Parameters.Select(p => new ParameterInfo
                    {
                        Name = p.Identifier.ValueText,
                        Type = p.Type?.ToString() ?? "var",
                        HasDefaultValue = p.Default != null
                    }).ToList()
                });
            }

            result.Classes.Add(classInfo);
        }

        // Extract interfaces
        var interfaces = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>();
        foreach (var iface in interfaces)
        {
            result.Interfaces.Add(new InterfaceInfo
            {
                Name = iface.Identifier.ValueText,
                Modifiers = iface.Modifiers.ToString(),
                BaseTypes = iface.BaseList?.Types.Select(t => t.Type.ToString()).ToList() ?? new List<string>(),
                Attributes = ExtractAttributes(iface.AttributeLists, ResolveConstValueFunc, result.ReferencedNamespaces)
            });
        }

        // Extract enums
        var enums = root.DescendantNodes().OfType<EnumDeclarationSyntax>();
        foreach (var enumDecl in enums)
        {
            result.Enums.Add(new EnumInfo
            {
                Name = enumDecl.Identifier.ValueText.GetCleanName(),
                Modifiers = enumDecl.Modifiers.ToString(),
                Attributes = ExtractAttributes(enumDecl.AttributeLists, ResolveConstValueFunc, result.ReferencedNamespaces),
                Members = enumDecl.Members.Select(m => m.Identifier.ValueText).ToList()
            });
        }

        return result;
    }

    // Pseudocode:
    // - When extracting positional attribute arguments, check if the argument is a string literal.
    // - If so, extract its value using the StringLiteralExpression's Token.ValueText property.
    // - Otherwise, use arg.ToString() as before.
    // - Replace: arguments.Add(arg.ToString());
    // - With: if (arg.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression)) { arguments.Add(literal.Token.ValueText); } else { arguments.Add(arg.ToString()); }

    private static List<AttributeInfo> ExtractAttributes(SyntaxList<AttributeListSyntax> attributeLists, Func<string, List<string>, string> ResolveConstValueFunc, List<string> references)
    {
        var attributes = new List<AttributeInfo>();

        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var arguments = new List<string>();
                var namedArguments = new Dictionary<string, string>();

                if (attribute.ArgumentList != null)
                {
                    foreach (var arg in attribute.ArgumentList.Arguments)
                    {
                        if (arg.NameEquals != null)
                        {
                            namedArguments[arg.NameEquals.Name.Identifier.ValueText] = GetArgumentValue(arg, ResolveConstValueFunc, references);
                        }
                        else if (arg.NameColon != null)
                        {
                            // Named argument with colon syntax: name: value
                            namedArguments[arg.NameColon.Name.Identifier.ValueText] = GetArgumentValue(arg, ResolveConstValueFunc, references);
                        }
                        else
                        {
                            arguments.Add(GetArgumentValue(arg, ResolveConstValueFunc, references));
                        }
                    }
                }

                attributes.Add(new AttributeInfo
                {
                    Name = attribute.Name.GetCleanName(),
                    Arguments =  arguments,
                    NamedArguments = namedArguments
                });
            }
        }

        return attributes;
    }

    // Add this helper method to resolve constant values using a SemanticModel
    private static string ResolveConstantValue(ExpressionSyntax expr, Func<string, List<string>, string> ResolveConstValueFunc, List<string> references)
    {
        var res = ResolveConstValueFunc.Invoke(expr.ToString(), references);

        if (!string.IsNullOrEmpty(res))
        {
            return res;
        }
        else
        {
            return expr.ToString();
        }
    }

    // Update GetArgumentValue to accept a SemanticModel and use ResolveConstantValue
    public static string GetArgumentValue(AttributeArgumentSyntax arg, Func<string, List<string>, string> ResolveConstValueFunc, List<string> references)
    {
        // Handle string literals directly
        if (arg.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }
        // Handle array initializers like new[] { "Admin", "User" }
        if (arg.Expression is ArrayCreationExpressionSyntax arrayCreation)
        {
            var initializer = arrayCreation.Initializer;
            if (initializer != null)
            {
                var values = initializer.Expressions
                    .OfType<LiteralExpressionSyntax>()
                    .Where(e => e.IsKind(SyntaxKind.StringLiteralExpression))
                    .Select(e => e.Token.ValueText);
                return $"[{string.Join(", ", values)}]";
            }
        }
        if (arg.Expression is ImplicitArrayCreationExpressionSyntax implicitArray)
        {
            var initializer = implicitArray.Initializer;
            if (initializer != null)
            {
                var values = initializer.Expressions
                    .OfType<LiteralExpressionSyntax>()
                    .Where(e => e.IsKind(SyntaxKind.StringLiteralExpression))
                    .Select(e => e.Token.ValueText);
                return $"[{string.Join(", ", values)}]";
            }
        }
        // Try to resolve constant value
        return ResolveConstantValue(arg.Expression, ResolveConstValueFunc, references);
    }
}