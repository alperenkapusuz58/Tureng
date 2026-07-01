using System.Collections.Concurrent;
using Kelimebull.Tts.Core.Data;

namespace ClockworkUmbraco.Services.Tts;

public sealed class TtsDispatchService
{
    private static readonly ConcurrentDictionary<string, byte> InFlight = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan ProcessingTimeout = TimeSpan.FromMinutes(2);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TtsDispatchService> _logger;
    private readonly string _workerId = $"http-{Environment.MachineName}";

    public TtsDispatchService(IServiceScopeFactory scopeFactory, ILogger<TtsDispatchService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void TryProcessInBackground(string contentHash)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
        {
            return;
        }

        if (!InFlight.TryAdd(contentHash, 0))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var registry = scope.ServiceProvider.GetRequiredService<ITtsAudioRegistry>();
                var processor = scope.ServiceProvider.GetRequiredService<TtsGenerationProcessor>();

                await registry.ReleaseAbandonedProcessingAsync(contentHash, CancellationToken.None);

                var item = await registry.TryClaimByHashAsync(
                    contentHash,
                    _workerId,
                    TimeSpan.FromMinutes(5),
                    CancellationToken.None);

                if (item is null)
                {
                    return;
                }

                _logger.LogInformation("On-demand TTS processing started for {ContentHash}", contentHash);

                using var timeoutCts = new CancellationTokenSource(ProcessingTimeout);
                await processor.ProcessAsync(item, timeoutCts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "On-demand TTS processing failed for {ContentHash}", contentHash);

                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var registry = scope.ServiceProvider.GetRequiredService<ITtsAudioRegistry>();
                    await registry.ReleaseAbandonedProcessingAsync(contentHash, CancellationToken.None);
                }
                catch (Exception releaseEx)
                {
                    _logger.LogWarning(releaseEx, "Failed to release abandoned TTS item for {ContentHash}", contentHash);
                }
            }
            finally
            {
                InFlight.TryRemove(contentHash, out _);
            }
        });
    }
}
