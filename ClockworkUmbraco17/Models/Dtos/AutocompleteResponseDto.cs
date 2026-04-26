namespace ClockworkUmbraco.Models.Dtos;

public class AutocompleteItemDto
{
    public string Lemma { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Translation { get; set; }
}

public class AutocompleteResponseDto
{
    public List<AutocompleteItemDto> Results { get; set; } = [];
}
