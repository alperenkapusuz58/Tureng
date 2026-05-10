using ClockworkUmbraco.Models.Dtos;

namespace ClockworkUmbraco.Services.Interfaces
{
    public interface ISearchService
    {
        /// <summary>Otomatik tamamlama için headword araması; token başına en az 3 karakter (sunucu filtresi).</summary>
        public SearchResponseModel Search(string q, string direction = "en-tr");

        /// <summary>Gevşetilmiş fuzzy/prefix sorgusu — sonuç bulunamadığında yakın madde başları için.</summary>
        public SearchResponseModel SearchSimilar(string q, string direction = "en-tr", int maxResults = 15);
    }
}

