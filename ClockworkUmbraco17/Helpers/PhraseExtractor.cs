using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace ClockworkUmbraco.Helpers;

/// <summary>Headword içindeki Idioms ve PhrasalVerbs bloklarından öbek metinlerini çıkarır.</summary>
public static class PhraseExtractor
{
    public static string[] GetSearchTokens(string value)
    {
        return value
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .SelectMany(token => token.Split(
                ['/', '\\', '-', '_', '.', ',', ';', ':', '(', ')', '[', ']', '{', '}', '"', '\''],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(token => token.Length > 0)
            .ToArray();
    }

    public static IEnumerable<string> GetPhrases(Headword headword)
    {
        foreach (var phrase in ExtractFromBlockList(headword.Idioms))
        {
            yield return phrase;
        }

        foreach (var phrase in ExtractFromBlockList(headword.PhrasalVerbs))
        {
            yield return phrase;
        }
    }

    public static IEnumerable<string> GetMatchingPhrases(Headword headword, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            yield break;
        }

        foreach (var phrase in GetPhrases(headword))
        {
            if (MatchesQuery(phrase, query))
            {
                yield return phrase;
            }
        }
    }

    public static bool MatchesQuery(string phrase, string query)
    {
        if (string.IsNullOrWhiteSpace(phrase) || string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        if (phrase.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var phraseTokens = GetSearchTokens(phrase);
        var queryTokens = GetSearchTokens(query);
        if (phraseTokens.Length == 0 || queryTokens.Length == 0)
        {
            return false;
        }

        var phraseText = string.Join(' ', phraseTokens);
        var queryText = string.Join(' ', queryTokens);
        if (phraseText.Contains(queryText, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var searchStart = 0;
        foreach (var queryToken in queryTokens)
        {
            var found = false;
            for (var i = searchStart; i < phraseTokens.Length; i++)
            {
                if (phraseTokens[i].Contains(queryToken, StringComparison.OrdinalIgnoreCase))
                {
                    searchStart = i + 1;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<string> ExtractFromBlockList(BlockListModel? blocks)
    {
        if (blocks == null)
        {
            yield break;
        }

        foreach (var block in blocks)
        {
            if (block.Content is not IdiomsAndProverbsItem item)
            {
                continue;
            }

            var phrase = item.Phrase?.Trim();
            if (!string.IsNullOrWhiteSpace(phrase))
            {
                yield return phrase;
            }
        }
    }
}
