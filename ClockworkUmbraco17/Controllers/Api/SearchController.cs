using System.Linq;
using ClockworkUmbraco.Helpers;
using ClockworkUmbraco.Models.Dtos;
using ClockworkUmbraco.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClockworkUmbraco.Controllers;

[ApiController]
[Route("api/dictionary")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly HeadwordSearchMapper _headwordSearchMapper;

    public SearchController(ISearchService searchService, HeadwordSearchMapper headwordSearchMapper)
    {
        _searchService = searchService;
        _headwordSearchMapper = headwordSearchMapper;
    }

    /// <summary>Madde başı (headword) araması. <c>ISearchService.Search</c>: External Index, <c>__NodeTypeAlias = headword</c>, alanlar <c>word</c>/<c>lemma</c> ve <c>nodeName</c>; terimler 3+ karakter.</summary>
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
        var wordResults = _headwordSearchMapper.MapSearchHitsToAutocompleteItems(searchResponse.SearchResults, query);
        var results = _headwordSearchMapper.MergeAutocompleteResults(wordResults, searchResponse.PhraseResults, query);

        #region agent log
        AgentDebugLog.Write(
            "SearchController.cs:39",
            "Search API merged autocomplete results",
            new
            {
                query,
                wordCount = wordResults.Count,
                phraseCount = searchResponse.PhraseResults.Count,
                total = results.Count,
                firstResults = results.Take(10).Select(x => new { x.Kind, x.Lemma, x.Url, x.Translation }).ToArray(),
            },
            "H3,H4");
        #endregion

        return Ok(new HeadwordSearchResponseDto
        {
            Query = searchResponse.Query,
            Total = results.Count,
            Results = results,
        });
    }

    /// <summary>Yakın madde başları — <c>ISearchService.SearchSimilar</c>: ilk token için fuzzy/prefix; Examine’da <c>headword</c> ile sınırlı.</summary>
    [HttpGet("similar")]
    [Produces("application/json")]
    public ActionResult<HeadwordSearchResponseDto> Similar(
        [FromQuery] string? q,
        [FromQuery] string? direction = "en-tr",
        [FromQuery] int take = 10)
    {
        take = Math.Clamp(take, 1, 50);
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(new HeadwordSearchResponseDto { Query = q?.Trim(), Total = 0, Results = [] });
        }

        var query = q.Trim();
        var searchResponse = _searchService.SearchSimilar(query, direction ?? "en-tr", take);
        var results = _headwordSearchMapper.MapSearchHitsToAutocompleteItems(searchResponse.SearchResults, query)
            .Take(take)
            .ToList();

        return Ok(new HeadwordSearchResponseDto
        {
            Query = query,
            Total = results.Count,
            Results = results,
        });
    }

}
