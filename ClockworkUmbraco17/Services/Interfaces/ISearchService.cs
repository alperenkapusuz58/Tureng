using ClockworkUmbraco.Models.Dtos;

namespace ClockworkUmbraco.Services.Interfaces
{
    public interface ISearchService
    {
        public SearchResponseModel Search(string q);

        /// <summary>
        /// Sadece <c>headword</c> dokümanlarında <c>lemma</c> ve <c>nodeName</c> alanlarında arama (Examine External Index).
        /// </summary>
        public SearchResponseModel SearchHeadwords(string q, int maxResults = 40);
    }
}

