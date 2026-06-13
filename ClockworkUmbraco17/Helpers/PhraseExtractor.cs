using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace ClockworkUmbraco.Helpers;

/// <summary>Headword içindeki Idioms ve PhrasalVerbs bloklarından öbek metinlerini çıkarır.</summary>
public static class PhraseExtractor
{
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
            if (phrase.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                yield return phrase;
            }
        }
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
