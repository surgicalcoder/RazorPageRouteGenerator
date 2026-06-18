using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GoLive.Generator.RazorPageRoute.Generator.Experimental;

internal sealed class RazorFileParseResult
{
    public string FileName { get; }
    public ImmutableArray<string> Routes { get; }
    public ImmutableArray<RazorAttribute> Attributes { get; }
    public ImmutableArray<CodeMember> Members { get; }
    public bool Excluded { get; }

    public RazorFileParseResult(string fileName, ImmutableArray<string> routes, ImmutableArray<RazorAttribute> attributes, ImmutableArray<CodeMember> members, bool excluded)
    {
        FileName = fileName;
        Routes = routes;
        Attributes = attributes;
        Members = members;
        Excluded = excluded;
    }
}

internal sealed record RazorAttribute(string Text);

internal sealed record CodeMember(
    string Kind,
    string Name,
    string TypeName,
    ImmutableArray<string> AttributeNames,
    ImmutableArray<string> AttributeTexts);

internal static class RazorFileParser
{
    private static readonly Regex PageDirectiveRegex = new(
        @"@page\s+""([^""]+)""",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex AttributeDirectiveRegex = new(
        @"@attribute\s+\[(.+?)\]\s*(?:\r?\n|$)",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex ExcludeAttributeRegex = new(
        @"@attribute\s+\[(?:.*\.)?ExcludeFromRouteGeneration(?:Attribute)?\s*\]",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex CodeBlockStartRegex = new(
        @"@(?:code|functions)\s*\{",
        RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex VerbatimStringRegex = new(
        @"@""[^""]*(?:""""[^""]*)*""",
        RegexOptions.Compiled);

    private static readonly Regex InterpolatedVerbatimStringRegex = new(
        @"\$@""[^""]*(?:""""[^""]*)*""",
        RegexOptions.Compiled);

    private static readonly Regex InterpolatedStringSimpleRegex = new(
        @"\$\""(?:[^\""\\]|\\.)*\""",
        RegexOptions.Compiled);

    public static RazorFileParseResult Parse(string fileName, string content)
    {
        var excluded = ExcludeAttributeRegex.IsMatch(content);
        var routes = ExtractRoutes(content);
        var attributes = ExtractAttributes(content);
        var codeBlock = ExtractCodeBlockContent(content);
        var members = codeBlock.Length > 0 ? ExtractCodeMembers(codeBlock) : [];

        return new RazorFileParseResult(fileName, routes, attributes, members, excluded);
    }

    private static ImmutableArray<string> ExtractRoutes(string content)
    {
        var matches = PageDirectiveRegex.Matches(content);
        if (matches.Count == 0) return [];

        var routes = new List<string>(matches.Count);
        foreach (Match match in matches)
        {
            var route = match.Groups[1].Value;
            if (!string.IsNullOrEmpty(route))
                routes.Add(route);
        }

        return routes.ToImmutableArray();
    }

    private static ImmutableArray<RazorAttribute> ExtractAttributes(string content)
    {
        var matches = AttributeDirectiveRegex.Matches(content);
        if (matches.Count == 0) return [];

        var attributes = new List<RazorAttribute>(matches.Count);
        foreach (Match match in matches)
        {
            var attrText = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(attrText))
                attributes.Add(new RazorAttribute(attrText));
        }

        return attributes.ToImmutableArray();
    }

    internal static string ExtractCodeBlockContent(string content)
    {
        var match = CodeBlockStartRegex.Match(content);
        if (!match.Success) return string.Empty;

        var startIndex = match.Index + match.Length;
        return ExtractBraceContent(content, startIndex);
    }

    internal static string ExtractBraceContent(string text, int startIndex)
    {
        if (startIndex >= text.Length) return string.Empty;

        int depth = 1;
        int i = startIndex;

        while (i < text.Length && depth > 0)
        {
            char c = text[i];

            if (i + 1 < text.Length)
            {
                string pair = text.Substring(i, 2);

                if (pair == "@\"" || pair == "$@")
                {
                    i = SkipVerbatimString(text, i);
                    continue;
                }
                if (pair == "$\"")
                {
                    i = SkipInterpolatedString(text, i);
                    continue;
                }
            }

            if (c == '"')
            {
                i++;
                while (i < text.Length)
                {
                    if (text[i] == '\\') { i += 2; continue; }
                    if (text[i] == '"') { i++; break; }
                    i++;
                }
                continue;
            }

            if (c == '\'')
            {
                i++;
                while (i < text.Length)
                {
                    if (text[i] == '\\') { i += 2; continue; }
                    if (text[i] == '\'') { i++; break; }
                    i++;
                }
                continue;
            }

            if (c == '/' && i + 1 < text.Length)
            {
                if (text[i + 1] == '/')
                {
                    i += 2;
                    while (i < text.Length && text[i] != '\n') i++;
                    continue;
                }
                if (text[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/')) i++;
                    if (i + 1 < text.Length) i += 2;
                    continue;
                }
            }

            if (c == '{') depth++;
            else if (c == '}') depth--;

            i++;
        }

        return text.Substring(startIndex, i - startIndex - (depth == 0 ? 1 : 0));
    }

    private static int SkipVerbatimString(string text, int i)
    {
        i += 2;
        while (i < text.Length)
        {
            if (text[i] == '"' && i + 1 < text.Length && text[i + 1] == '"')
            {
                i += 2;
                continue;
            }
            if (text[i] == '"') return i + 1;
            i++;
        }
        return i;
    }

    private static int SkipInterpolatedString(string text, int i)
    {
        i += 2;
        int depth = 0;
        while (i < text.Length)
        {
            char c = text[i];

            if (c == '\\' && i + 1 < text.Length) { i += 2; continue; }
            if (c == '{') { depth++; i++; continue; }
            if (c == '}' && depth > 0) { depth--; i++; continue; }
            if (c == '"' && depth == 0) return i + 1;

            i++;
        }
        return i;
    }

    private static ImmutableArray<CodeMember> ExtractCodeMembers(string codeBlockContent)
    {
        var wrappedCode = $@"using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.ComponentModel.DataAnnotations;
class _W {{ {codeBlockContent} }}";

        SyntaxTree tree;
        try
        {
            tree = CSharpSyntaxTree.ParseText(wrappedCode, new CSharpParseOptions(LanguageVersion.Latest));
        }
        catch
        {
            return [];
        }

        var root = tree.GetRoot();
        var classDecl = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == "_W");
        if (classDecl == null) return [];

        var members = new List<CodeMember>();

        foreach (var member in classDecl.Members)
        {
            var attrNames = member.AttributeLists
                .SelectMany(al => al.Attributes)
                .Select(a => a.Name.ToString())
                .ToImmutableArray();

            var attrTexts = member.AttributeLists
                .SelectMany(al => al.Attributes)
                .Select(a => a.ToFullString().Trim())
                .ToImmutableArray();

            switch (member)
            {
                case PropertyDeclarationSyntax prop:
                    members.Add(new CodeMember("property", prop.Identifier.Text, prop.Type.ToString(), attrNames, attrTexts));
                    break;
                case FieldDeclarationSyntax field:
                    foreach (var variable in field.Declaration.Variables)
                    {
                        members.Add(new CodeMember("field", variable.Identifier.Text, field.Declaration.Type.ToString(), attrNames, attrTexts));
                    }
                    break;
                case MethodDeclarationSyntax method:
                    members.Add(new CodeMember("method", method.Identifier.Text, method.ReturnType.ToString(), attrNames, attrTexts));
                    break;
            }
        }

        return members.ToImmutableArray();
    }
}
