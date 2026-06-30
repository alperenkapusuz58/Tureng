using Kelimebull.Tts.Core.Models;

namespace Kelimebull.Tts.Core.Data;

public interface ITtsAudioRegistry
{
    Task<TtsAudioRecord?> GetByHashAsync(string contentHash, CancellationToken cancellationToken = default);

    Task<TtsAudioRecord> EnsureQueuedAsync(TtsAudioDescriptor descriptor, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TtsQueueItem>> ClaimPendingAsync(string workerId, int batchSize, TimeSpan lockDuration, CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(string contentHash, string storageKey, string cdnUrl, string? openAiRequestId, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(string contentHash, string errorMessage, DateTimeOffset? nextAttemptUtc, CancellationToken cancellationToken = default);

    Task<TtsUsageSummary> GetUsageSummaryAsync(DateTimeOffset sinceUtc, CancellationToken cancellationToken = default);

    Task<int> ReplayFailedAsync(int maxItems, CancellationToken cancellationToken = default);

    Task<int> ReleaseStaleProcessingAsync(CancellationToken cancellationToken = default);
}

public sealed record TtsUsageSummary(
    int PendingCount,
    int ProcessingCount,
    int CompletedCount,
    int FailedCount,
    long CompletedCharacters,
    long QueuedCharacters);
