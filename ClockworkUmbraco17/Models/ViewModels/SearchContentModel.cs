namespace ClockworkUmbraco.Models.ViewModels
{
    public class SearchContentModel(IPublishedContent? content) : ContentModel(content)
    {
        public Dtos.SearchResponseModel? SearchResponse { get; set; }
    }
}

