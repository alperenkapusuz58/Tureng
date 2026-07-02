namespace Kelimebull.Tts.Core.Configuration;

public sealed class TtsOptions
{
    public const string SectionName = "Tts";

    public string PipelineVersion { get; set; } = "v6";

    public string CdnBaseUrl { get; set; } = string.Empty;

    public string OpenAiApiKey { get; set; } = string.Empty;

    public string DefaultModel { get; set; } = "tts-1-hd";

    public string DefaultFormat { get; set; } = "mp3";

    public int MaxTextLength { get; set; } = 500;

    public int DailyCharacterLimit { get; set; } = 20_000;

    public int MonthlyCharacterLimit { get; set; } = 400_000;

    public int RequestsPerMinute { get; set; } = 20;

    public int ApiRequestsPerMinutePerIp { get; set; } = 90;

    public int WorkerBatchSize { get; set; } = 10;

    public int WorkerParallelism { get; set; } = 1;

    public int MaxRetryAttempts { get; set; } = 3;

    public string HeadwordInstructions { get; set; } =
        "Pronounce this single English dictionary headword with clear dictionary pronunciation. "
        + "Articulate every syllable, including short unstressed initial vowels. "
        + "Speak only the word itself; do not add extra words.";

    public Dictionary<string, TtsLanguageOptions> Languages { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["English"] = new()
        {
            Language = "en-US",
            Model = "tts-1-hd",
            Voice = "nova",
        },
        ["Turkish"] = new()
        {
            Language = "tr-TR",
            Model = "tts-1",
            Voice = "alloy",
        },
    };

    public R2Options R2 { get; set; } = new();
}

public sealed class TtsLanguageOptions
{
    public string Language { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string Voice { get; set; } = string.Empty;

    public string? Instructions { get; set; }
}

public sealed class R2Options
{
    public string AccountId { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string AccessKeyId { get; set; } = string.Empty;

    public string SecretAccessKey { get; set; } = string.Empty;

    public string PublicBaseUrl { get; set; } = string.Empty;
}
