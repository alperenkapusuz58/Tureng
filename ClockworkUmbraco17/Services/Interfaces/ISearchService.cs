using ClockworkUmbraco.Models.Dtos;

namespace ClockworkUmbraco.Services.Interfaces
{
    public interface ISearchService
    {
        public SearchResponseModel Search(string q);
    }
}

