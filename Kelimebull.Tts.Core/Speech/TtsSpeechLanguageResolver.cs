using System.Text.RegularExpressions;

namespace Kelimebull.Tts.Core.Speech;

/// <summary>
/// Arama yönünden bağımsız olarak metnin okunacağı dili belirler.
/// </summary>
public static partial class TtsSpeechLanguageResolver
{
    private const string HeadwordSource = "word";
    private const string WordOfDaySource = "word-of-the-day";

    public static string Resolve(string? requestedLanguage, string normalizedText, string sourceType)
    {
        var source = sourceType.Trim().ToLowerInvariant();
        if (source is HeadwordSource or WordOfDaySource)
        {
            return "en-US";
        }

        if (TurkishLetterRegex().IsMatch(normalizedText))
        {
            return "tr-TR";
        }

        var request = NormalizeRequested(requestedLanguage);
        if (request is not null
            && request.StartsWith("tr", StringComparison.OrdinalIgnoreCase)
            && IsLatinScript(normalizedText))
        {
            return "en-US";
        }

        return request ?? "en-US";
    }

    private static bool IsLatinScript(string text)
    {
        foreach (var ch in text)
        {
            if (char.IsLetter(ch) && TurkishLetterRegex().IsMatch(ch.ToString()))
            {
                return false;
            }
        }

        return true;
    }

    private static string? NormalizeRequested(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var value = language.Trim();
        return value.Equals("tr", StringComparison.OrdinalIgnoreCase) ? "tr-TR" : value;
    }

    [GeneratedRegex(@"[çğıöşüÇĞİÖŞÜ]", RegexOptions.CultureInvariant)]
    private static partial Regex TurkishLetterRegex();
}
