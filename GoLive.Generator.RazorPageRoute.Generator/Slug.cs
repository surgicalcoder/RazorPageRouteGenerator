using System;
using System.Globalization;
using System.Text;

namespace GoLive.Generator.RazorPageRoute.Generator;

/// <summary>
/// Defines a set of utilities for creating slug urls.
/// </summary>
public static class Slug
{
    /// <summary>
    /// Creates a slug from the specified text.
    /// </summary>
    /// <param name="text">The text. If null if specified, null will be returned.</param>
    /// <returns>
    /// A slugged text.
    /// </returns>
    public static string Create(string text)
    {
        return Create(text, (SlugOptions)null);
    }

    /// <summary>
    /// Creates a slug from the specified text.
    /// </summary>
    /// <param name="text">The text. If null if specified, null will be returned.</param>
    /// <param name="options">The options. May be null.</param>
    /// <returns>A slugged text.</returns>
    public static string Create(string text, SlugOptions options)
    {
        if (text == null) return null;

        if (options == null)
        {
            options = new SlugOptions();
        }

        string normalised;

        if (options.EarlyTruncate && options.MaximumLength > 0 && text.Length > options.MaximumLength)
        {
            normalised = text.Substring(0, options.MaximumLength).Normalize(NormalizationForm.FormD);
        }
        else
        {
            normalised = text.Normalize(NormalizationForm.FormD);
        }

        int max = options.MaximumLength > 0 ? Math.Min(normalised.Length, options.MaximumLength) : normalised.Length;
        StringBuilder sb = new StringBuilder(max);

        for (int i = 0; i < normalised.Length; i++)
        {
            char c = normalised[i];
            UnicodeCategory uc = char.GetUnicodeCategory(c);

            if (options.AllowedUnicodeCategories.Contains(uc) && options.IsAllowed(c))
            {
                switch (uc)
                {
                    case UnicodeCategory.UppercaseLetter:
                        if (options.ToLower)
                        {
                            c = options.Culture != null ? char.ToLower(c, options.Culture) : char.ToLowerInvariant(c);
                        }

                        sb.Append(options.Replace(c));

                        break;

                    case UnicodeCategory.LowercaseLetter:
                        if (options.ToUpper)
                        {
                            c = options.Culture != null ? char.ToUpper(c, options.Culture) : char.ToUpperInvariant(c);
                        }

                        sb.Append(options.Replace(c));

                        break;

                    default:
                        sb.Append(options.Replace(c));

                        break;
                }
            }
            else if (uc == UnicodeCategory.NonSpacingMark)
            {
                // don't add a separator
            }
            else
            {
                if (options.Separator != null && !EndsWith(sb, options.Separator))
                {
                    sb.Append(options.Separator);
                }
            }

            if (options.MaximumLength > 0 && sb.Length >= options.MaximumLength) break;
        }

        string result = sb.ToString();

        if (options.MaximumLength > 0 && result.Length > options.MaximumLength)
        {
            result = result.Substring(0, options.MaximumLength);
        }

        if (!options.CanEndWithSeparator && options.Separator != null && result.EndsWith(options.Separator))
        {
            result = result.Substring(0, result.Length - options.Separator.Length);
        }

        return result.Normalize(NormalizationForm.FormC);
    }

    private static bool EndsWith(StringBuilder sb, string text)
    {
        if (sb.Length < text.Length) return false;

        for (int i = 0; i < text.Length; i++)
        {
            if (sb[sb.Length - 1 - i] != text[text.Length - 1 - i]) return false;
        }

        return true;
    }
}