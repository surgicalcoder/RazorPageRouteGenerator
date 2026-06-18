using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GoLive.Generator.RazorPageRoute.Generator.Experimental;

public class SlugOptions
{
    public bool EarlyTruncate { get; set; }
    public int MaximumLength { get; set; }
    public string Separator { get; set; } = "_";
    public bool ToLower { get; set; } = true;
    public bool ToUpper { get; set; }
    public CultureInfo Culture { get; set; }
    public bool CanEndWithSeparator { get; set; }
    public HashSet<UnicodeCategory> AllowedUnicodeCategories { get; set; } = new()
    {
        UnicodeCategory.UppercaseLetter,
        UnicodeCategory.LowercaseLetter,
        UnicodeCategory.DecimalDigitNumber
    };

    public HashSet<char> AllowedChars { get; set; } = [];
    public HashSet<char> DeniedChars { get; set; } = [];

    private HashSet<char> _deniedWithReplacements;
    private Dictionary<char, string> _replacements = [];

    public void AddReplacement(char denied, string replacement)
    {
        _replacements[denied] = replacement;
        DeniedChars.Add(denied);
    }

    public string Replace(char c)
    {
        if (_replacements.TryGetValue(c, out var replacement))
        {
            return replacement;
        }

        return c.ToString();
    }

    public bool IsAllowed(char c)
    {
        if (DeniedChars.Contains(c))
        {
            return false;
        }

        if (!AllowedChars.Contains(c) && AllowedChars.Count > 0)
        {
            return false;
        }

        return true;
    }

    public SlugOptions()
    {
    }
}
