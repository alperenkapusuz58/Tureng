using ClockworkUmbraco.Extensions;
using ClockworkUmbraco.Examine;
using ClockworkUmbraco.Helpers;
using ClockworkUmbraco.Models.Dtos;
using ClockworkUmbraco.Services.Interfaces;
using Examine;
using Examine.Search;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace ClockworkUmbraco.Services
{
    /// <summary>External Examine indeksi üzerinden headword (madde başı) araması.</summary>
    public class SearchService : ISearchService
    {
        /// <summary>
        /// Umbraco External Index’te madde metni için beklenen alanlar: yayında <c>word</c>; geçmiş uyumluluk için <c>lemma</c>.
        /// </summary>
        private static readonly string[] HeadwordTextFields = ["word", "lemma"];

        private const int ExamineMaxHits = 400;

        private readonly IExamineManager _examineManager;
        private readonly IPublishedContentQuery _publishedContentQuery;
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly IPublishedValueFallback _publishedValueFallback;

        /// <summary>Yönlendirme dışı içerik tipleri (headword hariç — madde araması headword ile pozitif filtrelenir).</summary>
        private readonly string[] _docTypesToExclude =
            [DictionaryNoResults.ModelTypeAlias, MainPage.ModelTypeAlias, SiteSettings.ModelTypeAlias];

        public SearchService(
            IExamineManager examineManager,
            IPublishedContentQuery publishedContentQuery,
            IVariationContextAccessor variationContextAccessor,
            IPublishedValueFallback publishedValueFallback)
        {
            _examineManager = examineManager ?? throw new ArgumentNullException(nameof(examineManager));
            _publishedContentQuery = publishedContentQuery ?? throw new ArgumentNullException(nameof(publishedContentQuery));
            _variationContextAccessor = variationContextAccessor;
            _publishedValueFallback = publishedValueFallback;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Yalnızca 3+ karakterlik tokenlarla arama yapılır; aksi halde geniş indeks taraması yapılmaz.
        /// Examine: <c>__NodeTypeAlias = headword</c>, <see cref="HeadwordTextFields"/> ve <c>nodeName</c>.
        /// </remarks>
        public SearchResponseModel Search(string q, string direction = "en-tr")
        {
            _variationContextAccessor.VariationContext = new VariationContext(direction == "en-tr" ? "en" : "tr");
            var trimmed = q.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || !_examineManager.TryGetIndex(UmbracoConstants.UmbracoIndexes.ExternalIndexName, out IIndex? index))
            {
                return new SearchResponseModel();
            }

            string[] terms = trimmed.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length > 2).ToArray();

            if (terms.Length == 0)
            {
                // Kısa terimler (örn. tek harf "a") ExternalIndex'in StandardAnalyzer stopword'lerine
                // takıldığından burada bulunamaz. Enter ile tam eşleşen madde başına gidebilmek için
                // stopword içermeyen InternalIndex üzerinden birebir eşleşme araması yapılır.
                return SearchExactShortTerm(trimmed);
            }

            IBooleanOperation query = index.Searcher.CreateQuery(IndexTypes.Content)
                .GroupedNot(["hide"], ["1"])
                .And().GroupedNot(["__NodeTypeAlias"], _docTypesToExclude)
                .And().Field("__NodeTypeAlias", Headword.ModelTypeAlias);

            query.And().Group(
                inner => inner
                    .GroupedOr(HeadwordTextFields, terms.Boost(80))
                    .Or()
                    .GroupedOr(["nodeName"], terms.Boost(70))
                    .Or()
                    .GroupedOr(HeadwordTextFields, terms.Fuzzy())
                    .Or()
                    .GroupedOr(HeadwordTextFields, terms.MultipleCharacterWildcard())
                    .Or()
                    .GroupedOr(["nodeName"], terms.Fuzzy())
                    .Or()
                    .GroupedOr(["nodeName"], terms.MultipleCharacterWildcard()),
                BooleanOperation.Or);

            ISearchResults pageOfResults = query.Execute();

            var filteredResults = pageOfResults
                .Take(ExamineMaxHits)
                .Where(result =>
                {
                    var contentItem = _publishedContentQuery.Content(result.Id);
                    return contentItem?.TemplateId != null;
                })
                .ToList();

            var phraseResults = SearchPhrases(trimmed);

            return new SearchResponseModel(trimmed, filteredResults.Count, filteredResults)
            {
                PhraseResults = phraseResults,
            };
        }

        /// <inheritdoc />
        /// <remarks>Examine: <c>__NodeTypeAlias = headword</c>; İlk anlamlı token fuzzy/prefix ile eşleştirilir.</remarks>
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

            IBooleanOperation boolQuery = index.Searcher.CreateQuery(IndexTypes.Content)
                .GroupedNot(["hide"], ["1"])
                .And().GroupedNot(["__NodeTypeAlias"], _docTypesToExclude)
                .And().Field("__NodeTypeAlias", Headword.ModelTypeAlias);

            string[] full = [token];
            boolQuery.And().Group(
                inner =>
                {
                    var branch = inner
                        .GroupedOr(HeadwordTextFields, full.Fuzzy(fuzzySimilarity))
                        .Or()
                        .GroupedOr(["nodeName"], full.Fuzzy(fuzzySimilarity))
                        .Or()
                        .GroupedOr(HeadwordTextFields, full.MultipleCharacterWildcard())
                        .Or()
                        .GroupedOr(["nodeName"], full.MultipleCharacterWildcard());

                    if (token.Length >= 3)
                    {
                        var prefixLen = Math.Min(4, token.Length);
                        string[] prefixTerms = [token.Substring(0, prefixLen)];
                        branch = branch
                            .Or()
                            .GroupedOr(HeadwordTextFields, prefixTerms.MultipleCharacterWildcard())
                            .Or()
                            .GroupedOr(["nodeName"], prefixTerms.MultipleCharacterWildcard());
                    }

                    return branch;
                },
                BooleanOperation.Or);

            ISearchResults pageOfResults = boolQuery.Execute();

            var filteredResults = pageOfResults
                .Take(ExamineMaxHits)
                .Where(result =>
                {
                    var contentItem = _publishedContentQuery.Content(result.Id);
                    return contentItem?.TemplateId != null;
                })
                .Take(maxResults * 5)
                .ToList();

            return new SearchResponseModel(q.Trim(), filteredResults.Count, filteredResults);
        }

        /// <summary>
        /// Kısa terimler (3 karakterden az, örn. tek harf "a") için birebir eşleşme araması.
        /// ExternalIndex'in StandardAnalyzer stopword'leri bu terimleri elediğinden, stopword
        /// içermeyen <see cref="UmbracoConstants.UmbracoIndexes.InternalIndexName"/> üzerinden aranır.
        /// </summary>
        private SearchResponseModel SearchExactShortTerm(string trimmed)
        {
            if (!_examineManager.TryGetIndex(UmbracoConstants.UmbracoIndexes.InternalIndexName, out IIndex? index))
            {
                return new SearchResponseModel(trimmed, 0, Array.Empty<ISearchResult>());
            }

            string[] exact = [trimmed];

            IBooleanOperation query = index.Searcher.CreateQuery(IndexTypes.Content)
                .GroupedNot(["hide"], ["1"])
                .And().GroupedNot(["__NodeTypeAlias"], _docTypesToExclude)
                .And().Field("__NodeTypeAlias", Headword.ModelTypeAlias);

            query.And().Group(
                inner => inner
                    .GroupedOr(HeadwordTextFields, exact)
                    .Or()
                    .GroupedOr(["nodeName"], exact),
                BooleanOperation.Or);

            ISearchResults pageOfResults = query.Execute();

            var filteredResults = pageOfResults
                .Take(ExamineMaxHits)
                .Where(result =>
                {
                    var contentItem = _publishedContentQuery.Content(result.Id);
                    return contentItem?.TemplateId != null;
                });

            return new SearchResponseModel(trimmed, filteredResults.Count(), filteredResults);
        }

        /// <summary>
        /// InternalIndex <c>phrases</c> alanında öbek araması; eşleşen öbekler autocomplete DTO olarak döner.
        /// </summary>
        private List<AutocompleteItemDto> SearchPhrases(string trimmed)
        {
            var internalIndexFound = _examineManager.TryGetIndex(UmbracoConstants.UmbracoIndexes.InternalIndexName, out IIndex? index);

            #region agent log
            AgentDebugLog.Write(
                "SearchService.cs:221",
                "Phrase search entry",
                new { query = trimmed, queryLength = trimmed.Length, internalIndexFound },
                "H1,H2",
                "post-fix");
            #endregion

            if (trimmed.Length < 3 || !internalIndexFound || index == null)
            {
                return [];
            }

            var tokens = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return [];
            }

            // Öbekler madde başının (headword) Idioms/PhrasalVerbs bloklarında yaşar ve öbek metni
            // genelde madde başı kelimesiyle başlar. Bu yüzden sorgu tokenlarıyla word/nodeName üzerinden
            // aday headword'leri buluyor, ardından published içerikten "içeren" (contains) filtresiyle
            // eşleşen öbekleri seçiyoruz. Bu yol özel index alanına (ve rebuild'e) bağımlı değildir.
            IBooleanOperation query = index.Searcher.CreateQuery(IndexTypes.Content)
                .GroupedNot(["hide"], ["1"])
                .And().GroupedNot(["__NodeTypeAlias"], _docTypesToExclude)
                .And().Field("__NodeTypeAlias", Headword.ModelTypeAlias);

            query.And().Group(
                inner => inner
                    .GroupedOr(HeadwordTextFields, tokens.MultipleCharacterWildcard())
                    .Or()
                    .GroupedOr(["nodeName"], tokens.MultipleCharacterWildcard()),
                BooleanOperation.Or);

            ISearchResults pageOfResults = query.Execute();
            var candidateResults = pageOfResults.Take(ExamineMaxHits).ToList();

            #region agent log
            AgentDebugLog.Write(
                "SearchService.cs:258",
                "InternalIndex phrase candidate results",
                new
                {
                    query = trimmed,
                    tokens,
                    candidateCount = candidateResults.Count,
                    candidateIds = candidateResults.Take(10).Select(x => x.Id).ToArray(),
                },
                "H1,H2",
                "post-fix");
            #endregion

            var phraseItems = new List<AutocompleteItemDto>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var result in candidateResults)
            {
                var contentItem = _publishedContentQuery.Content(result.Id);
                if (contentItem?.TemplateId == null || contentItem.ContentType.Alias != Headword.ModelTypeAlias)
                {
                    continue;
                }

                var headword = new Headword(contentItem, _publishedValueFallback);
                var headwordUrl = headword.Url();
                if (string.IsNullOrEmpty(headwordUrl))
                {
                    continue;
                }

                var headwordLabel = headword.Word?.Trim();
                if (string.IsNullOrEmpty(headwordLabel))
                {
                    headwordLabel = contentItem.Name ?? string.Empty;
                }

                foreach (var phrase in PhraseExtractor.GetMatchingPhrases(headword, trimmed))
                {
                    var dedupeKey = $"{headwordUrl}|{phrase}";
                    if (!seen.Add(dedupeKey))
                    {
                        continue;
                    }

                    var anchor = PhraseAnchor.ToHash(phrase);
                    phraseItems.Add(new AutocompleteItemDto
                    {
                        Kind = "phrase",
                        Lemma = phrase,
                        Translation = headwordLabel,
                        Url = string.IsNullOrEmpty(anchor) ? headwordUrl : $"{headwordUrl}#{anchor}",
                    });
                }
            }

            #region agent log
            AgentDebugLog.Write(
                "SearchService.cs:302",
                "Phrase search mapped items",
                new
                {
                    query = trimmed,
                    phraseItemCount = phraseItems.Count,
                    items = phraseItems.Take(10).Select(x => new { x.Lemma, x.Url, x.Translation }).ToArray(),
                },
                "H2,H3,H4",
                "post-fix");
            #endregion

            return phraseItems;
        }
    }
}
