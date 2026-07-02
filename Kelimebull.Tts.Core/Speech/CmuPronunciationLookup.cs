using System.Reflection;
using System.Text.RegularExpressions;

namespace Kelimebull.Tts.Core.Speech;

public sealed partial class CmuPronunciationLookup : ICmuPronunciationLookup
{
    private const string DictionaryResourceName = "Kelimebull.Tts.Core.Data.cmudict.dict";
    private readonly Dictionary<string, string[]> _phonesByWord;

    public CmuPronunciationLookup()
    {
        _phonesByWord = LoadDictionary();
    }

    public bool TryGetPhones(string word, out IReadOnlyList<string> phones)
    {
        phones = Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        var key = NormalizeLookupKey(word);
        if (!_phonesByWord.TryGetValue(key, out var found))
        {
            return false;
        }

        phones = found;
        return true;
    }

    public bool HasUnstressedInitialVowel(string word)
    {
        if (!TryGetPhones(word, out var phones) || phones.Count == 0)
        {
            return false;
        }

        return IsUnstressedVowelPhone(phones[0]);
    }

    internal static bool IsUnstressedVowelPhone(string phone)
    {
        if (phone.Length < 2)
        {
            return false;
        }

        var stress = phone[^1];
        if (stress != '0')
        {
            return false;
        }

        var basePhone = phone[..^1];
        return basePhone is
            "AA" or "AE" or "AH" or "AO" or "AW" or "AY" or
            "EH" or "ER" or "EY" or "IH" or "IY" or
            "OW" or "OY" or "UH" or "UW" or "AX";
    }

    private static string NormalizeLookupKey(string word)
    {
        var trimmed = word.Trim();
        var withoutVariant = VariantSuffixRegex().Replace(trimmed, string.Empty);
        return withoutVariant.ToLowerInvariant();
    }

    private static Dictionary<string, string[]> LoadDictionary()
    {
        var assembly = typeof(CmuPronunciationLookup).Assembly;
        using var stream = assembly.GetManifestResourceStream(DictionaryResourceName)
            ?? throw new InvalidOperationException($"Embedded CMUdict resource not found: {DictionaryResourceName}");

        using var reader = new StreamReader(stream);
        var dictionary = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0 || line.StartsWith(";;;", StringComparison.Ordinal))
            {
                continue;
            }

            var hashIndex = line.IndexOf('#');
            if (hashIndex >= 0)
            {
                line = line[..hashIndex];
            }

            line = line.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var separatorIndex = line.IndexOf(' ');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var rawWord = line[..separatorIndex];
            var key = NormalizeLookupKey(rawWord);
            if (dictionary.ContainsKey(key))
            {
                continue;
            }

            var phones = line[(separatorIndex + 1)..]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (phones.Length == 0)
            {
                continue;
            }

            dictionary[key] = phones;
        }

        return dictionary;
    }

    [GeneratedRegex(@"\(\d+\)$", RegexOptions.CultureInvariant)]
    private static partial Regex VariantSuffixRegex();
}
