using System.Text.RegularExpressions;

namespace Kelimebull.Tts.Core.Speech;

/// <summary>
/// Madde başı kelimeler için TTS girdisini hazırlar.
/// CMUdict ile vurgusuz ilk hece tespit edilir; schwa ise "uh, kelime" öneki eklenir.
/// </summary>
public sealed partial class TtsHeadwordSpeechInputBuilder : ITtsHeadwordSpeechInputBuilder
{
    private const string HeadwordSource = "word";
    private const string WordOfDaySource = "word-of-the-day";

    private readonly ICmuPronunciationLookup _pronunciationLookup;

    public TtsHeadwordSpeechInputBuilder(ICmuPronunciationLookup pronunciationLookup)
    {
        _pronunciationLookup = pronunciationLookup;
    }

    public string Build(string normalizedText, string language, string sourceType)
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
        if (word.Length < 2 || word.Contains(' '))
        {
            return normalizedText;
        }

        if (!LatinWordRegex().IsMatch(word))
        {
            return normalizedText;
        }

        if (NeedsSchwaClarification(word))
        {
            return $"uh, {word}";
        }

        return normalizedText;
    }

    private bool NeedsSchwaClarification(string word)
    {
        if (_pronunciationLookup.TryGetPhones(word, out _))
        {
            return _pronunciationLookup.HasUnstressedInitialVowel(word);
        }

        return NeedsSchwaClarificationHeuristic(word.ToLowerInvariant());
    }

    /// <summary>CMUdict'te bulunamayan kelimeler için yedek heuristik.</summary>
    private static bool NeedsSchwaClarificationHeuristic(string lower)
    {
        if (lower.Length < 3 || lower[0] != 'a' || !IsConsonant(lower[1]))
        {
            return false;
        }

        if (StrongInitialSchwaDenylist.Contains(lower))
        {
            return false;
        }

        if (ShortSchwaInitialAllowlist.Contains(lower))
        {
            return true;
        }

        return lower.Length >= 5;
    }

    private static readonly HashSet<string> StrongInitialSchwaDenylist = new(StringComparer.Ordinal)
    {
        "amber", "angry", "apple", "apron", "arrow", "aster", "attic", "actor", "atlas", "azure",
    };

    private static readonly HashSet<string> ShortSchwaInitialAllowlist = new(StringComparer.Ordinal)
    {
        "ago", "away",
    };

    private static bool IsConsonant(char ch)
        => char.IsAsciiLetter(ch) && !IsVowel(ch);

    private static bool IsVowel(char ch)
        => ch is 'a' or 'e' or 'i' or 'o' or 'u' or 'y';

    [GeneratedRegex(@"^[A-Za-z][A-Za-z'-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex LatinWordRegex();
}
