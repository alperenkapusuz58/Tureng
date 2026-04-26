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
public class DictionarySearchApiController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly UmbracoHelper _umbracoHelper;
    private readonly IPublishedValueFallback _publishedValueFallback;

    public DictionarySearchApiController(
        ISearchService searchService,
        UmbracoHelper umbracoHelper,
        IPublishedValueFallback publishedValueFallback)
    {
        _searchService = searchService;
        _umbracoHelper = umbracoHelper;
        _publishedValueFallback = publishedValueFallback;
    }

    /// <summary>Ana sayfa arama kutusu otomatik tamamlama (Examine).</summary>
    [HttpGet("autocomplete")]
    public ActionResult<AutocompleteResponseDto> Autocomplete([FromQuery] string? q, [FromQuery] string? direction = "en-tr")
    {
        _ = direction;
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return Ok(new AutocompleteResponseDto());
        }

        var response = _searchService.SearchHeadwords(q.Trim(), 12);
        var dto = new AutocompleteResponseDto();
        foreach (var hit in response.SearchResults ?? Enumerable.Empty<ISearchResult>())
        {
            var c = _umbracoHelper.Content(hit.Id);
            if (c == null)
            {
                continue;
            }

            if (c.ContentType.Alias != Headword.ModelTypeAlias)
            {
                continue;
            }

            if (c.TemplateId == null)
            {
                continue;
            }

            var hw = new Headword(c, _publishedValueFallback);
            dto.Results.Add(new AutocompleteItemDto
            {
                Lemma = hw.Lemma ?? string.Empty,
                Url = hw.Url() ?? string.Empty,
                Translation = HeadwordDisplay.FirstTranslation(hw),
            });
        }

        return Ok(dto);
    }
}
