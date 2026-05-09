using ClockworkUmbraco.Models.Dtos;
using Examine;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace ClockworkUmbraco.Helpers;

/// <summary>Examine headword vuruşlarını autocomplete DTO listesine çevirir (arama API ve görünümler için).</summary>
public class HeadwordSearchMapper
{
    private readonly IPublishedValueFallback _publishedValueFallback;
    private readonly UmbracoHelper _umbracoHelper;

    public HeadwordSearchMapper(IPublishedValueFallback publishedValueFallback, UmbracoHelper umbracoHelper)
    {
        _publishedValueFallback = publishedValueFallback;
        _umbracoHelper = umbracoHelper;
    }

    public List<AutocompleteItemDto> MapSearchHitsToAutocompleteItems(IEnumerable<ISearchResult>? hits, string queryText)
    {
        var results = new List<AutocompleteItemDto>();

        foreach (var hit in hits ?? Enumerable.Empty<ISearchResult>())
        {
            IPublishedContent? c = null;
            if (int.TryParse(hit.Id, out var nodeId))
            {
                c = _umbracoHelper.Content(nodeId);
            }
            else if (Guid.TryParse(hit.Id, out var nodeKey))
            {
                c = _umbracoHelper.Content(nodeKey);
            }

            if (c == null || c.ContentType.Alias != Headword.ModelTypeAlias || c.TemplateId == null)
            {
                continue;
            }

            var headword = new Headword(c, _publishedValueFallback);
            var translation = HeadwordDisplay.FirstTranslation(headword);
            var lemmaText = headword.Word?.Trim();
            if (string.IsNullOrEmpty(lemmaText))
            {
                lemmaText = c.Name ?? string.Empty;
            }

            results.Add(new AutocompleteItemDto
            {
                Lemma = lemmaText,
                Url = headword.Url() ?? string.Empty,
                Translation = translation,
            });
        }

        return results
            .GroupBy(x => x.Lemma, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(x => GetSearchScore(x, queryText))
            .ThenBy(x => x.Lemma, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int GetSearchScore(AutocompleteItemDto item, string query)
    {
        if (string.Equals(item.Lemma, query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (item.Lemma.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (!string.IsNullOrWhiteSpace(item.Translation)
            && string.Equals(item.Translation, query, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (!string.IsNullOrWhiteSpace(item.Translation)
            && item.Translation.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (item.Lemma.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (!string.IsNullOrWhiteSpace(item.Translation)
            && item.Translation.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        return 6;
    }
}
