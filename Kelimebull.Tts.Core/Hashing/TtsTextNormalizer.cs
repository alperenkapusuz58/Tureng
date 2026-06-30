using System.Net;
using System.Text.RegularExpressions;

namespace Kelimebull.Tts.Core.Hashing;

public static partial class TtsTextNormalizer
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var decoded = WebUtility.HtmlDecode(text);
        var withoutTags = HtmlTagRegex().Replace(decoded, " ");
        var collapsed = WhitespaceRegex().Replace(withoutTags, " ").Trim();
        var withStraightQuotes = NormalizeQuotes(collapsed);
        var spelledQuotedAbbreviations = SpellQuotedAbbreviations(withStraightQuotes);
        var expanded = ExpandAbbreviations(spelledQuotedAbbreviations);
        return WhitespaceRegex().Replace(expanded, " ").Trim();
    }

    private static string NormalizeQuotes(string text)
        => text
            .Replace('\u2018', '\'')
            .Replace('\u2019', '\'')
            .Replace('\u201C', '"')
            .Replace('\u201D', '"');

    private static string SpellQuotedAbbreviations(string text)
        => QuotedAbbreviationRegex().Replace(text, static match =>
            string.Join(" ", match.Groups[1].Value.ToUpperInvariant().ToCharArray()));

    private static string ExpandAbbreviations(string text)
    {
        var result = text;
        foreach (var (pattern, replacement) in AbbreviationExpansions)
        {
            result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return result;
    }

    private static readonly (string Pattern, string Replacement)[] AbbreviationExpansions =
    [
        (@"\bDr\.?\b", "Doctor"),
        (@"\bMr\.?\b", "Mister"),
        (@"\bMrs\.?\b", "Missus"),
        (@"\bMs\.?\b", "Miss"),
        (@"\bSt\.?\b", "Saint"),
        (@"\bProf\.?\b", "Professor"),
        (@"\betc\.?\b", "et cetera"),
    ];

    [GeneratedRegex(@"""([A-Za-z]{2,4})""", RegexOptions.CultureInvariant)]
    private static partial Regex QuotedAbbreviationRegex();

    [GeneratedRegex("<.*?>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
