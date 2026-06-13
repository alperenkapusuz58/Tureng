namespace ClockworkUmbraco.Models.Dtos;

public class AutocompleteItemDto
{
    /// <summary><c>word</c> madde başı, <c>phrase</c> deyim/öbek.</summary>
    public string Kind { get; set; } = "word";

    public string Lemma { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Translation { get; set; }
}

public class AutocompleteResponseDto
{
    public List<AutocompleteItemDto> Results { get; set; } = [];
}
