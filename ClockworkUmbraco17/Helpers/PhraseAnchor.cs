using System.Text.RegularExpressions;

namespace ClockworkUmbraco.Helpers;

/// <summary>Öbek (phrase) anchor hash'leri — arama URL'i ve Headword view id'leri aynı slug'ı kullanır.</summary>
public static partial class PhraseAnchor
{
    public const string HashPrefix = "phrase-";

    public static string Slug(string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return string.Empty;
        }

        var normalized = phrase.Trim().ToLowerInvariant();
        var slug = NonAlphaNumeric().Replace(normalized, "-").Trim('-');
        return slug;
    }

    public static string ToHash(string? phrase)
    {
        var slug = Slug(phrase);
        return string.IsNullOrEmpty(slug) ? string.Empty : HashPrefix + slug;
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonAlphaNumeric();
}
