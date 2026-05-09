using ClockworkUmbraco.Extensions;
using ClockworkUmbraco.Models.Dtos;
using ClockworkUmbraco.Services.Interfaces;
using Examine;
using Examine.Search;
using Umbraco.Cms.Core;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace ClockworkUmbraco.Services
{
    public class SearchService : ISearchService
    {
        private readonly IExamineManager _examineManager;
        private readonly IPublishedContentQuery _publishedContentQuery;
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly string[] _docTypesToExclude =
            [];

        public SearchService(IExamineManager examineManager, IPublishedContentQuery publishedContentQuery, IVariationContextAccessor variationContextAccessor)
        {
            _examineManager = examineManager ?? throw new ArgumentNullException(nameof(examineManager));
            _publishedContentQuery = publishedContentQuery ?? throw new ArgumentNullException(nameof(publishedContentQuery));
            _variationContextAccessor = variationContextAccessor;
        }
        public SearchResponseModel Search(string q, string direction = "en-tr")
        {
            _variationContextAccessor.VariationContext = new VariationContext(direction == "en-tr" ? "en" : "tr");
            if (string.IsNullOrWhiteSpace(q) || !_examineManager.TryGetIndex(UmbracoConstants.UmbracoIndexes.ExternalIndexName, out IIndex? index))
            {
                return new SearchResponseModel();
            }

            IBooleanOperation? query = index.Searcher.CreateQuery(IndexTypes.Content)
                .GroupedNot(["hide"], ["1"])
                .And().GroupedNot(["__NodeTypeAlias"], _docTypesToExclude);

            string[]? terms = !string.IsNullOrWhiteSpace(q)
           ? q.Split(" ", StringSplitOptions.RemoveEmptyEntries)
           .Where(x => x.Length > 2).ToArray() : null;


            if (terms != null && terms.Length > 0)
            {
                query!.And().Group(q => q
                    .GroupedOr(["lemma"], terms.Boost(80))
                    .Or()
                    .GroupedOr(["nodeName"], terms.Boost(70))
                    .Or()
                    .GroupedOr(["lemma"], terms.Fuzzy())
                    .Or()
                    .GroupedOr(["lemma"], terms.MultipleCharacterWildcard())
                    .Or()
                    .GroupedOr(["nodeName"], terms.Fuzzy())
                    .Or()
                    .GroupedOr(["nodeName"], terms.MultipleCharacterWildcard()

                    ), BooleanOperation.Or);
            }

            ISearchResults? pageOfResults = query.Execute();

            var filteredResults = pageOfResults.Where(result =>
            {
                var contentItem = _publishedContentQuery.Content(result.Id);
                return contentItem?.TemplateId != null;
            });

            return new SearchResponseModel(q, filteredResults.Count(), filteredResults);
        }

        /// <inheritdoc />
        public SearchResponseModel SearchSimilar(string q, string direction = "en-tr", int maxResults = 15)
        {
            _variationContextAccessor.VariationContext = new VariationContext(direction == "en-tr" ? "en" : "tr");
            if (string.IsNullOrWhiteSpace(q) || !_examineManager.TryGetIndex(UmbracoConstants.UmbracoIndexes.ExternalIndexName, out IIndex? index))
            {
                return new SearchResponseModel();
            }

            var token = (q.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(token))
            {
                return new SearchResponseModel();
            }

            maxResults = Math.Clamp(maxResults, 1, 50);
            const float fuzzySimilarity = 0.72f;

            IBooleanOperation? boolQuery = index.Searcher.CreateQuery(IndexTypes.Content)
                .GroupedNot(["hide"], ["1"])
                .And().GroupedNot(["__NodeTypeAlias"], _docTypesToExclude);

            string[] full = [token];
            boolQuery!.And().Group(
                inner =>
                {
                    var branch = inner
                        .GroupedOr(["lemma"], full.Fuzzy(fuzzySimilarity))
                        .Or()
                        .GroupedOr(["nodeName"], full.Fuzzy(fuzzySimilarity))
                        .Or()
                        .GroupedOr(["lemma"], full.MultipleCharacterWildcard())
                        .Or()
                        .GroupedOr(["nodeName"], full.MultipleCharacterWildcard());

                    if (token.Length >= 3)
                    {
                        var prefixLen = Math.Min(4, token.Length);
                        string[] prefixTerms = [token.Substring(0, prefixLen)];
                        branch = branch
                            .Or()
                            .GroupedOr(["lemma"], prefixTerms.MultipleCharacterWildcard())
                            .Or()
                            .GroupedOr(["nodeName"], prefixTerms.MultipleCharacterWildcard());
                    }

                    return branch;
                },
                BooleanOperation.Or);

            ISearchResults pageOfResults = boolQuery.Execute();

            const int examineCap = 400;
            var filteredResults = pageOfResults
                .Take(examineCap)
                .Where(result =>
                {
                    var contentItem = _publishedContentQuery.Content(result.Id);
                    return contentItem?.TemplateId != null
                        && string.Equals(contentItem.ContentType.Alias, Headword.ModelTypeAlias, StringComparison.OrdinalIgnoreCase);
                })
                .Take(maxResults * 5)
                .ToList();

            return new SearchResponseModel(q.Trim(), filteredResults.Count, filteredResults);
        }
    }
}

