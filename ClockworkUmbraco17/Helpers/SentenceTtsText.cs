using System.Net;
using System.Text.RegularExpressions;

namespace ClockworkUmbraco.Helpers;

/// <summary>RTE örnek cümlelerinden yalnızca ilk paragrafı TTS metni olarak çıkarır.</summary>
public static partial class SentenceTtsText
{
    public static string GetFirstParagraphPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var match = FirstParagraphRegex().Match(html);
        var blockHtml = match.Success ? match.Groups[1].Value : html;
        var decoded = WebUtility.HtmlDecode(blockHtml);
        var withoutTags = HtmlTagRegex().Replace(decoded, " ");
        return WhitespaceRegex().Replace(withoutTags, " ").Trim();
    }

    [GeneratedRegex(@"<p[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex FirstParagraphRegex();

    [GeneratedRegex("<.*?>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}
