namespace ClockworkUmbraco.Models.Dtos;

/// <summary>GET api/dictionary/search JSON yanıtı — yalnızca headword (madde başı) sonuçları.</summary>
public class HeadwordSearchResponseDto
{
    public string? Query { get; set; }
    public long Total { get; set; }
    public List<AutocompleteItemDto> Results { get; set; } = [];
}
