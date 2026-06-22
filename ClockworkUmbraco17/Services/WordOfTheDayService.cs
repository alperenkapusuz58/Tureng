using ClockworkUmbraco.Services.Interfaces;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace ClockworkUmbraco.Services;

public class WordOfTheDayService : IWordOfTheDayService
{
    private readonly IPublishedContentQuery _publishedContentQuery;
    private readonly IPublishedValueFallback _publishedValueFallback;

    public WordOfTheDayService(
        IPublishedContentQuery publishedContentQuery,
        IPublishedValueFallback publishedValueFallback)
    {
        _publishedContentQuery = publishedContentQuery;
        _publishedValueFallback = publishedValueFallback;
    }

    public Headword? GetWordOfTheDay(DateTime? date = null)
    {
        var today = (date ?? DateTime.Now).Date;

        var headwordNodes = _publishedContentQuery.ContentAtRoot()
            .SelectMany(root => root.DescendantsOfType(Headword.ModelTypeAlias))
            .Where(node => node.TemplateId != null && !string.IsNullOrWhiteSpace(node.Value<string>("word")))
            .OrderBy(node => node.Id)
            .ToList();

        if (headwordNodes.Count == 0)
        {
            return null;
        }

        var index = today.DayOfYear % headwordNodes.Count;
        return new Headword(headwordNodes[index], _publishedValueFallback);
    }
}
