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
        return WhitespaceRegex().Replace(withoutTags, " ").Trim();
    }

    [GeneratedRegex("<.*?>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex("\\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}
