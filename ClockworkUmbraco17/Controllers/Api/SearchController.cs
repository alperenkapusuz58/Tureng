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
        var results = _headwordSearchMapper.MapSearchHitsToAutocompleteItems(searchResponse.SearchResults, query);

        return Ok(new HeadwordSearchResponseDto
        {
            Query = searchResponse.Query,
            Total = results.Count,
            Results = results,
        });
    }

    /// <summary>Yakın madde başları — gevşetilmiş Examine sorgusu (<c>ISearchService.SearchSimilar</c>).</summary>
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
