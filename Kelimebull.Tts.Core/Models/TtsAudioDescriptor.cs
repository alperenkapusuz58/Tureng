namespace Kelimebull.Tts.Core.Models;

public sealed record TtsAudioDescriptor(
    string ContentHash,
    string OriginalText,
    string NormalizedText,
    string Language,
    string Voice,
    string Model,
    string Format,
    string PipelineVersion,
    string SourceType,
    int CharacterCount,
    string StorageKey);

public sealed record TtsAudioRecord(
    long Id,
    string ContentHash,
    string OriginalText,
    string NormalizedText,
    string Language,
    string Voice,
    string Model,
    string Format,
    string PipelineVersion,
    string SourceType,
    string Status,
    int CharacterCount,
    string? StorageKey,
    string? CdnUrl,
    string? OpenAiRequestId,
    string? ErrorMessage,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc,
    DateTimeOffset? CompletedUtc);

public sealed record TtsQueueItem(
    long Id,
    string ContentHash,
    string OriginalText,
    string NormalizedText,
    string Language,
    string Voice,
    string Model,
    string Format,
    string PipelineVersion,
    string SourceType,
    int CharacterCount,
    string StorageKey,
    int AttemptCount);

public static class TtsStatuses
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
