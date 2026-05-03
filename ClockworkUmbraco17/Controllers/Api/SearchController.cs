using System.Linq;
using ClockworkUmbraco.Helpers;
using ClockworkUmbraco.Models.Dtos;
using ClockworkUmbraco.Services.Interfaces;
using Examine;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace ClockworkUmbraco.Controllers;

[ApiController]
[Route("api/dictionary")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly IPublishedValueFallback _publishedValueFallback;
    private readonly UmbracoHelper _umbracoHelper;

    public SearchController(
        ISearchService searchService,
        IPublishedValueFallback publishedValueFallback,
        UmbracoHelper umbracoHelper)
    {
        _searchService = searchService;
        _publishedValueFallback = publishedValueFallback;
        _umbracoHelper = umbracoHelper;
    }

    /// <summary>Madde başı (headword) araması. Examine <c>__NodeTypeAlias = headword</c>.</summary>
    [HttpGet("search")]
    [Produces("application/json")]
    public ActionResult<HeadwordSearchResponseDto> Search(
        [FromQuery] string? q,
        [FromQuery] string? direction = "en-tr")
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new HeadwordSearchResponseDto { Query = q?.Trim(), Total = 0, Results = [] });
        }

        var query = q.Trim();
        var searchResponse = _searchService.Search(query, direction ?? "en-tr");
        var results = new List<AutocompleteItemDto>();

        foreach (var hit in searchResponse.SearchResults ?? Enumerable.Empty<ISearchResult>())
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
            results.Add(new AutocompleteItemDto
            {
                Lemma = headword.Word ?? string.Empty,
                Url = headword.Url() ?? string.Empty,
                Translation = translation,
            });
        }

        results = results
            .OrderBy(x => GetSearchScore(x, query))
            .ThenBy(x => x.Lemma, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Ok(new HeadwordSearchResponseDto
        {
            Query = searchResponse.Query,
            Total = searchResponse.TotalResultCount,
            Results = results,
        });
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
