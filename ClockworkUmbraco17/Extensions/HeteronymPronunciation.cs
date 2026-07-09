public static class HeteronymPronunciation
{
    private static readonly Dictionary<(string Word, string Pos), string> IpaOverrides =
        new()
        {
            [("advocate", "noun")] = "ˈædvəkət",
            [("advocate", "verb")] = "ˈædvəkeɪt",
            [("record", "noun")] = "ˈrɛkɔːrd",
            [("record", "verb")] = "rɪˈkɔːrd",
            [("present", "noun")] = "ˈprɛzənt",
            [("present", "verb")] = "prɪˈzɛnt",
        };

    public static string? ResolveIpa(string word, string partOfSpeech)
    {
        var key = ((word ?? "").Trim().ToLowerInvariant(), (partOfSpeech ?? "").Trim().ToLowerInvariant());
        return IpaOverrides.TryGetValue(key, out var ipa) ? ipa : null;
    }
}