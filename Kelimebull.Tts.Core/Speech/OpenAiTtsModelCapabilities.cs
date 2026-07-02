namespace Kelimebull.Tts.Core.Speech;

public static class OpenAiTtsModelCapabilities
{
    public static bool SupportsInstructions(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        return model.Trim().StartsWith("gpt-4o-mini-tts", StringComparison.OrdinalIgnoreCase);
    }
}
