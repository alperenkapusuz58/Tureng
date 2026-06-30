using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;
using Kelimebull.Tts.Core.Data;
using Kelimebull.Tts.Core.Exceptions;
using Kelimebull.Tts.Core.Models;

namespace ClockworkUmbraco.Services.Tts;

public sealed class TtsGenerationProcessor
{
    private readonly ITtsAudioRegistry _registry;
    private readonly IOpenAiTtsClient _openAiTtsClient;
    private readonly IR2AudioStorage _storage;
    private readonly TtsRateLimiter _rateLimiter;
    private readonly TtsOptions _options;
    private readonly ILogger<TtsGenerationProcessor> _logger;

    public TtsGenerationProcessor(
        ITtsAudioRegistry registry,
        IOpenAiTtsClient openAiTtsClient,
        IR2AudioStorage storage,
        TtsRateLimiter rateLimiter,
        IOptions<TtsOptions> options,
        ILogger<TtsGenerationProcessor> logger)
    {
        _registry = registry;
        _openAiTtsClient = openAiTtsClient;
        _storage = storage;
        _rateLimiter = rateLimiter;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessAsync(TtsQueueItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureBudgetAsync(item, cancellationToken);
            await _rateLimiter.WaitAsync(cancellationToken);

            var result = await _openAiTtsClient.GenerateAsync(item, cancellationToken);
            var url = await _storage.UploadAsync(item.StorageKey, ResolveContentType(item.Format), result.AudioBytes, cancellationToken);
            await _registry.MarkCompletedAsync(item.ContentHash, item.StorageKey, url, result.RequestId, cancellationToken);
        }
        catch (TtsBudgetLimitException budgetEx)
        {
            _logger.LogWarning(
                "TTS budget limit reached for {ContentHash}. Next attempt: {NextAttemptUtc}",
                item.ContentHash,
                budgetEx.RetryAfterUtc);
            await _registry.MarkFailedAsync(item.ContentHash, budgetEx.Message, budgetEx.RetryAfterUtc, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            DateTimeOffset? nextAttemptUtc = item.AttemptCount >= _options.MaxRetryAttempts
                ? null
                : DateTimeOffset.UtcNow.Add(GetBackoff(item.AttemptCount));

            _logger.LogWarning(ex, "TTS generation failed for {ContentHash}. Next attempt: {NextAttemptUtc}", item.ContentHash, nextAttemptUtc);
            await _registry.MarkFailedAsync(item.ContentHash, ex.Message, nextAttemptUtc, cancellationToken);
        }
    }

    private async Task EnsureBudgetAsync(TtsQueueItem item, CancellationToken cancellationToken)
    {
        var daySummary = await _registry.GetUsageSummaryAsync(DateTimeOffset.UtcNow.Date, cancellationToken);
        if (daySummary.CompletedCharacters + item.CharacterCount > _options.DailyCharacterLimit)
        {
            var retryAfterUtc = DateTimeOffset.UtcNow.Date.AddDays(1);
            throw new TtsBudgetLimitException(
                "Günlük seslendirme limiti doldu. Yarın otomatik olarak tekrar denenecek.",
                retryAfterUtc);
        }

        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var monthSummary = await _registry.GetUsageSummaryAsync(monthStart, cancellationToken);
        if (monthSummary.CompletedCharacters + item.CharacterCount > _options.MonthlyCharacterLimit)
        {
            var retryAfterUtc = monthStart.AddMonths(1);
            throw new TtsBudgetLimitException(
                "Aylık seslendirme limiti doldu. Gelecek ay otomatik olarak tekrar denenecek.",
                retryAfterUtc);
        }
    }

    private static TimeSpan GetBackoff(int attemptCount)
    {
        var minutes = Math.Min(60, Math.Pow(2, Math.Max(0, attemptCount - 1)));
        return TimeSpan.FromMinutes(minutes);
    }

    private static string ResolveContentType(string format)
    {
        return format.Equals("wav", StringComparison.OrdinalIgnoreCase) ? "audio/wav" : "audio/mpeg";
    }
}
