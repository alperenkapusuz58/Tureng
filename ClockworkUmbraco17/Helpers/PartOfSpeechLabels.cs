namespace ClockworkUmbraco.Helpers;

/// <summary>
/// CMS / paneldeki kelime türü (Türkçe veya İngilizce) değerlerini arayüzde İngilizce slug ile göstermek için.
/// FRONTEND/scripts/pos-labels.js ile aynı mantık.
/// </summary>
public static class PartOfSpeechLabels
{
    private static readonly Dictionary<string, string> TrToEn = new(StringComparer.OrdinalIgnoreCase)
    {
        ["isim"] = "noun",
        ["fiil"] = "verb",
        ["sıfat"] = "adjective",
        ["zarf"] = "adverb",
        ["zamir"] = "pronoun",
        ["edat"] = "preposition",
        ["bağlaç"] = "conjunction",
        ["ünlem"] = "interjection",
    };

    private static readonly Dictionary<string, string> EnAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["n"] = "noun",
        ["v"] = "verb",
        ["adj"] = "adjective",
        ["adv"] = "adverb",
        ["pron"] = "pronoun",
        ["prep"] = "preposition",
        ["conj"] = "conjunction",
        ["interj"] = "interjection",
    };

    /// <summary>
    /// Örn. "İsim", "noun", "adj" → "noun"; bilinmiyorsa küçük harf ham değer.
    /// </summary>
    public static string ToEnglishSlug(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var trimmed = raw.Trim();
        var trKey = trimmed.ToLowerInvariant();
        if (TrToEn.TryGetValue(trKey, out var fromTr))
        {
            return fromTr;
        }

        trKey = trimmed.ToLower(new System.Globalization.CultureInfo("tr-TR"));
        if (TrToEn.TryGetValue(trKey, out fromTr))
        {
            return fromTr;
        }

        var lower = trimmed.ToLowerInvariant();
        if (TrToEn.TryGetValue(lower, out fromTr))
        {
            return fromTr;
        }

        if (EnAliases.TryGetValue(lower, out var fromAlias))
        {
            return fromAlias;
        }

        var noSpace = lower.Replace(" ", "", StringComparison.Ordinal);
        if (EnAliases.TryGetValue(noSpace, out fromAlias))
        {
            return fromAlias;
        }

        return lower;
    }
}
