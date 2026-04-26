using ClockworkUmbraco.Extensions;
using ClockworkUmbraco.Models.Dtos;
using ClockworkUmbraco.Services.Interfaces;
using Examine;
using Examine.Search;
using Lucene.Net.Analysis.Core;
using Umbraco.Cms.Core;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace ClockworkUmbraco.Services
{
    public class SearchService : ISearchService
    {
        private readonly IExamineManager _examineManager;
        private readonly IPublishedContentQuery _publishedContentQuery;
        private readonly string[] _docTypesToExclude =
            [];

        public SearchService(IExamineManager examineManager, IPublishedContentQuery publishedContentQuery)
        {
            _examineManager = examineManager ?? throw new ArgumentNullException(nameof(examineManager));
            _publishedContentQuery = publishedContentQuery ?? throw new ArgumentNullException(nameof(publishedContentQuery));
        }

        /// <inheritdoc />
        public SearchResponseModel SearchHeadwords(string q, int maxResults = 40)
        {
            if (string.IsNullOrWhiteSpace(q)
                || !_examineManager.TryGetIndex(UmbracoConstants.UmbracoIndexes.ExternalIndexName, out IIndex? index))
            {
                return new SearchResponseModel();
            }

            var trimmed = q.Trim();
            if (trimmed.Length == 0)
            {
                return new SearchResponseModel();
            }

            var terms = trimmed.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !StopAnalyzer.ENGLISH_STOP_WORDS_SET.Contains(x.ToLowerInvariant()) && x.Length >= 1)
                .ToArray();

            if (terms.Length == 0)
            {
                terms = [trimmed];
            }

            IBooleanOperation query = index.Searcher.CreateQuery(IndexTypes.Content)
                .GroupedNot(["hide"], ["1"])
                .And()
                .Field("__NodeTypeAlias", Headword.ModelTypeAlias);

            query.And().Group(inner => inner
                .GroupedOr(["lemma"], terms.Boost(90))
                .Or()
                .GroupedOr(["nodeName"], terms.Boost(85))
                .Or()
                .GroupedOr(["lemma"], terms.Fuzzy(0.6f))
                .Or()
                .GroupedOr(["nodeName"], terms.Fuzzy(0.6f))
                .Or()
                .GroupedOr(["lemma"], terms.MultipleCharacterWildcard())
                .Or()
                .GroupedOr(["nodeName"], terms.MultipleCharacterWildcard()), BooleanOperation.Or);

            var raw = query.Execute();
            var filtered = raw
                .Where(result =>
                {
                    var contentItem = _publishedContentQuery.Content(result.Id);
                    return contentItem?.TemplateId != null;
                })
                .Take(maxResults)
                .ToList();

            return new SearchResponseModel(trimmed, filtered.Count, filtered);
        }
        public SearchResponseModel Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || !_examineManager.TryGetIndex(UmbracoConstants.UmbracoIndexes.ExternalIndexName, out IIndex? index))
            {
                return new SearchResponseModel();
            }

            IBooleanOperation? query = index.Searcher.CreateQuery(IndexTypes.Content)
                .GroupedNot(["hide"], ["1"])
                .And().GroupedNot(["__NodeTypeAlias"], _docTypesToExclude);

            string[]? terms = !string.IsNullOrWhiteSpace(q)
           ? q.Split(" ", StringSplitOptions.RemoveEmptyEntries)
           .Where(x => !StopAnalyzer.ENGLISH_STOP_WORDS_SET.Contains(x.ToLower()) && x.Length > 2).ToArray() : null;


            if (terms != null && terms.Length > 0)
            {
                query!.And().Group(q => q
                    .GroupedOr(["title"], terms.Boost(80))
                    .Or()
                    .GroupedOr(["nodeName"], terms.Boost(70))
                    .Or()
                    .GroupedOr(["title"], terms.Fuzzy())
                    .Or()
                    .GroupedOr(["title"], terms.MultipleCharacterWildcard())
                    .Or()
                    .GroupedOr(["nodeName"], terms.Fuzzy())
                    .Or()
                    .GroupedOr(["nodeName"], terms.MultipleCharacterWildcard())
                    .Or()
                    .GroupedOr(["description"], terms.Boost(50))
                    .Or()
                    .GroupedOr(["article"], terms.Boost(40)

                    ), BooleanOperation.Or);
            }

            ISearchResults? pageOfResults = query.Execute();

            // Filter out results that don't have a template
            var filteredResults = pageOfResults.Where(result =>
            {
                var contentItem = _publishedContentQuery.Content(result.Id);
                return contentItem?.TemplateId != null;
            });

            return new SearchResponseModel(q, filteredResults.Count(), filteredResults);
        }
    }
}

