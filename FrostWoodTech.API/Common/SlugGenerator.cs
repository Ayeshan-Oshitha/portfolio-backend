using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FrostWoodTech.API.Common;

/// <summary>
/// Slugs are lowercase and hyphenated. Uniqueness is the service layer's job — this only shapes
/// the string.
/// </summary>
public static partial class SlugGenerator
{
    public static string Generate(string input)
    {
        // Strip accents so "Café" becomes "cafe" rather than losing the letter entirely.
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        var slug = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        slug = NonSlugCharacters().Replace(slug, "-");
        slug = RepeatedHyphens().Replace(slug, "-");

        return slug.Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugCharacters();

    [GeneratedRegex("-{2,}")]
    private static partial Regex RepeatedHyphens();
}
