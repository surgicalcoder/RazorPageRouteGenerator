using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GoLive.Generator.RazorPageRoute.Generator;

public static class AttributeExtensions
{
    /// <summary>
    /// Returns the attribute name as string, removing any 'global::' prefix.
    /// </summary>
    public static string GetCleanName(this NameSyntax nameSyntax)
    {
        var name = nameSyntax.ToString();
        return name.StartsWith("global::") ? name["global::".Length..] : name;
    }

    /// <summary>
    /// Returns the string, removing any 'global::' prefix.
    /// </summary>
    public static string GetCleanName(this string nameSyntax)
    {
        return nameSyntax.StartsWith("global::") ? nameSyntax["global::".Length..] : nameSyntax;
    }
}
