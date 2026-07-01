using System.Text.RegularExpressions;

namespace Kelimebull.Tts.Core.Speech;

/// <summary>
/// Madde başı kelimeler için TTS girdisini sözlük telaffuzuna uygun hale getirir.
/// Zayıf başlangıç hecelerini (ör. abandon, accept) tire ile ayırarak modelin ilk sesi yutmasını azaltır.
/// </summary>
public static partial class TtsHeadwordSpeechInput
{
    private const string HeadwordSource = "word";
    private const string WordOfDaySource = "word-of-the-day";

    public static string Build(string normalizedText, string language, string sourceType)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            return string.Empty;
        }

        var source = sourceType.Trim().ToLowerInvariant();
        if (source is not (HeadwordSource or WordOfDaySource))
        {
            return normalizedText;
        }

        if (!language.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedText;
        }

        var word = normalizedText.Trim();
        if (word.Length < 3 || word.Contains(' '))
        {
            return normalizedText;
        }

        if (!LatinWordRegex().IsMatch(word))
        {
            return normalizedText;
        }

        var lower = word.ToLowerInvariant();
        if (lower[0] == 'a' && IsConsonant(lower[1]))
        {
            return $"a-{word[1..]}";
        }

        return normalizedText;
    }

    private static bool IsConsonant(char ch)
        => char.IsAsciiLetter(ch) && !IsVowel(ch);

    private static bool IsVowel(char ch)
        => ch is 'a' or 'e' or 'i' or 'o' or 'u' or 'y';

    [GeneratedRegex(@"^[A-Za-z][A-Za-z'-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex LatinWordRegex();
}
