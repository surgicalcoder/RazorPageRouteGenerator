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
    public static AnalysisResult AnalyzeSourceFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Source file not found: {filePath}");
        }

        var sourceCode = File.ReadAllText(filePath);
        return AnalyzeSourceCode(sourceCode);
    }

    public static AnalysisResult AnalyzeSourceCode(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = syntaxTree.GetCompilationUnitRoot();

        var result = new AnalysisResult();

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
                Attributes = ExtractAttributes(cls.AttributeLists)
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
                    Attributes = ExtractAttributes(prop.AttributeLists),
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
                        Attributes = ExtractAttributes(field.AttributeLists),
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
                    Attributes = ExtractAttributes(method.AttributeLists),
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
                Attributes = ExtractAttributes(iface.AttributeLists)
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
                Attributes = ExtractAttributes(enumDecl.AttributeLists),
                Members = enumDecl.Members.Select(m => m.Identifier.ValueText).ToList()
            });
        }

        return result;
    }

    private static List<AttributeInfo> ExtractAttributes(SyntaxList<AttributeListSyntax> attributeLists)
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
                            // Named argument: name = value
                            namedArguments[arg.NameEquals.Name.Identifier.ValueText] = arg.Expression.ToString();
                        }
                        else if (arg.NameColon != null)
                        {
                            // Named argument with colon syntax: name: value
                            namedArguments[arg.NameColon.Name.Identifier.ValueText] = arg.Expression.ToString();
                        }
                        else
                        {
                            // Positional argument
                            arguments.Add(arg.ToString());
                        }
                    }
                }

                attributes.Add(new AttributeInfo
                {
                    Name = attribute.Name.GetCleanName(),
                    Arguments = arguments,
                    NamedArguments = namedArguments
                });
            }
        }

        return attributes;
    }
}