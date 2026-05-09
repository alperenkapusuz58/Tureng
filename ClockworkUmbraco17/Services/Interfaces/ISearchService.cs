using ClockworkUmbraco.Models.Dtos;

namespace ClockworkUmbraco.Services.Interfaces
{
    public interface ISearchService
    {
        public SearchResponseModel Search(string q, string direction = "en-tr");

        /// <summary>Gevşetilmiş fuzzy/prefix sorgusu — sonuç bulunamadığında yakın madde başları için.</summary>
        public SearchResponseModel SearchSimilar(string q, string direction = "en-tr", int maxResults = 15);
    }
}

