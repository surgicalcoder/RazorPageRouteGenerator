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
    public static AnalysisResult AnalyzeSourceFile(string filePath, SemanticModel semanticModel)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Source file not found: {filePath}");
        }

        var sourceCode = File.ReadAllText(filePath);
        return AnalyzeSourceCode(sourceCode, semanticModel);
    }

    public static AnalysisResult AnalyzeSourceCode(string sourceCode, SemanticModel semanticModel)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = syntaxTree.GetCompilationUnitRoot();

        var result = new AnalysisResult();

        // Extract referenced namespaces
        var usingDirectives = root.Usings.Select(u => u.Name?.ToString()).ToList();
        result.ReferencedNamespaces.AddRange(usingDirectives);

        // Extract namespaces
        var namespaces = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();
        foreach (var ns in namespaces)
        {
            result.Namespaces.Add(new NamespaceInfo
            {
                Name = ns.Name.ToString(),
                FullName = ns.Name.ToString()
            });
        }

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
                Attributes = ExtractAttributes(cls.AttributeLists, semanticModel)
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
                    Attributes = ExtractAttributes(prop.AttributeLists, semanticModel),
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
                        Attributes = ExtractAttributes(field.AttributeLists, semanticModel),
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
                    Attributes = ExtractAttributes(method.AttributeLists, semanticModel),
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
                Attributes = ExtractAttributes(iface.AttributeLists, semanticModel)
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
                Attributes = ExtractAttributes(enumDecl.AttributeLists, semanticModel),
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

    private static List<AttributeInfo> ExtractAttributes(SyntaxList<AttributeListSyntax> attributeLists, SemanticModel semanticModel)
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
                            namedArguments[arg.NameEquals.Name.Identifier.ValueText] = GetArgumentValue(arg, semanticModel);
                        }
                        else if (arg.NameColon != null)
                        {
                            // Named argument with colon syntax: name: value
                            namedArguments[arg.NameColon.Name.Identifier.ValueText] = GetArgumentValue(arg, semanticModel);
                        }
                        else
                        {
                            arguments.Add(GetArgumentValue(arg, semanticModel));
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
        private static string ResolveConstantValue(SemanticModel semanticModel, ExpressionSyntax expr)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expr);
            if (symbolInfo.Symbol is IFieldSymbol { HasConstantValue: true } fieldSymbol)
            {
                return fieldSymbol.ConstantValue?.ToString();
            }
            // For properties, try to get the constant value via ConstantValue property on the syntax (not available for IPropertySymbol)
            // .NET Standard 2.0 Roslyn does not support constant value for properties, so fallback to ToString()
            return expr.ToString();
        }

    // Update GetArgumentValue to accept a SemanticModel and use ResolveConstantValue
    public static string GetArgumentValue(AttributeArgumentSyntax arg, SemanticModel semanticModel)
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
        return ResolveConstantValue(semanticModel, arg.Expression);
    }
}