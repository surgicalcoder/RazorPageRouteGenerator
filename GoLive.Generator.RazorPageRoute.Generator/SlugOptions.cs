using System.Collections.Generic;
using System.Globalization;

namespace GoLive.Generator.RazorPageRoute.Generator;

/// <summary>
/// Defines options for the Slug utility class.
/// </summary>
public class SlugOptions
{
    /// <summary>
    /// Defines the default maximum length. Currently equal to 80.
    /// </summary>
    public const int DefaultMaximumLength = 80;

    /// <summary>
    /// Defines the default separator. Currently equal to "-".
    /// </summary>
    public const string DefaultSeparator = "_";

    private bool _toLower;
    private bool _toUpper;

    /// <summary>
    /// Initializes a new instance of the <see cref="SlugOptions"/> class.
    /// </summary>
    public SlugOptions()
    {
        MaximumLength = DefaultMaximumLength;
        Separator = DefaultSeparator;
        AllowedUnicodeCategories = new List<UnicodeCategory>();
        AllowedUnicodeCategories.Add(UnicodeCategory.UppercaseLetter);
        AllowedUnicodeCategories.Add(UnicodeCategory.LowercaseLetter);
        AllowedUnicodeCategories.Add(UnicodeCategory.DecimalDigitNumber);
        AllowedRanges = new List<KeyValuePair<short, short>>();
        AllowedRanges.Add(new KeyValuePair<short, short>((short)'a', (short)'z'));
        AllowedRanges.Add(new KeyValuePair<short, short>((short)'A', (short)'Z'));
        AllowedRanges.Add(new KeyValuePair<short, short>((short)'0', (short)'9'));
    }

    /// <summary>
    /// Gets the allowed unicode categories list.
    /// </summary>
    /// <value>
    /// The allowed unicode categories list.
    /// </value>
    public virtual IList<UnicodeCategory> AllowedUnicodeCategories { get; private set; }

    /// <summary>
    /// Gets the allowed ranges list.
    /// </summary>
    /// <value>
    /// The allowed ranges list.
    /// </value>
    public virtual IList<KeyValuePair<short, short>> AllowedRanges { get; private set; }

    /// <summary>
    /// Gets or sets the maximum length.
    /// </summary>
    /// <value>
    /// The maximum length.
    /// </value>
    public virtual int MaximumLength { get; set; }

    /// <summary>
    /// Gets or sets the separator.
    /// </summary>
    /// <value>
    /// The separator.
    /// </value>
    public virtual string Separator { get; set; }

    /// <summary>
    /// Gets or sets the culture for case conversion.
    /// </summary>
    /// <value>
    /// The culture.
    /// </value>
    public virtual CultureInfo Culture { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the string can end with a separator string.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the string can end with a separator string; otherwise, <c>false</c>.
    /// </value>
    public virtual bool CanEndWithSeparator { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the string is truncated before normalization.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the string is truncated before normalization; otherwise, <c>false</c>.
    /// </value>
    public virtual bool EarlyTruncate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to lowercase the resulting string.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the resulting string must be lowercased; otherwise, <c>false</c>.
    /// </value>
    public virtual bool ToLower
    {
        get { return _toLower; }
        set
        {
            _toLower = value;

            if (_toLower)
            {
                _toUpper = false;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to uppercase the resulting string.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the resulting string must be uppercased; otherwise, <c>false</c>.
    /// </value>
    public virtual bool ToUpper
    {
        get { return _toUpper; }
        set
        {
            _toUpper = value;

            if (_toUpper)
            {
                _toLower = false;
            }
        }
    }

    /// <summary>
    /// Determines whether the specified character is allowed.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <returns>true if the character is allowed; false otherwise.</returns>
    public virtual bool IsAllowed(char character)
    {
        foreach (var p in AllowedRanges)
        {
            if (character >= p.Key && character <= p.Value) return true;
        }

        return false;
    }

    /// <summary>
    /// Replaces the specified character by a given string.
    /// </summary>
    /// <param name="character">The character to replace.</param>
    /// <returns>a string.</returns>
    public virtual string Replace(char character)
    {
        return character.ToString();
    }
}