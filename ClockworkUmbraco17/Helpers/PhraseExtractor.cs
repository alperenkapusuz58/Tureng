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
    /// <summary>
    /// Sorgudaki her kelimenin, öbek içindeki kelimelerden en az biriyle (sırayla, kelime
    /// BAŞINDAN itibaren) eşleşip eşleşmediğini kontrol eder.
    /// </summary>
    /// <remarks>
    /// Kasıtlı olarak düz alt-dize (Contains) yerine <c>StartsWith</c> kullanılır. Aksi halde
    /// "and" gibi kısa bir sorgu, "abandon", "brand", "understand" gibi kelimelerin İÇİNDE
    /// geçtiği için yanlışlıkla eşleşir. StartsWith ile "and" yalnızca "and", "andalusia" gibi
    /// gerçekten o harflerle BAŞLAYAN kelimelere eşleşir; "and all" / "above and beyond" gibi
    /// meşru öbek eşleşmeleri ise korunur (çünkü oradaki "and" kelimesi kendi başına bir token'dır).
    /// </remarks>
    public static bool MatchesQuery(string phrase, string query)
    {
        if (string.IsNullOrWhiteSpace(phrase) || string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var phraseTokens = GetSearchTokens(phrase);
        var queryTokens = GetSearchTokens(query);
        if (phraseTokens.Length == 0 || queryTokens.Length == 0)
        {
            return false;
        }

        var searchStart = 0;
        foreach (var queryToken in queryTokens)
        {
            var found = false;
            for (var i = searchStart; i < phraseTokens.Length; i++)
            {
                if (phraseTokens[i].StartsWith(queryToken, StringComparison.OrdinalIgnoreCase))
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
