using Kelimebull.Tts.Core.Data;

namespace ClockworkUmbraco.Services.Tts;

/// <summary>
/// SQL erişimi olmadan takılı TTS kayıtlarını periyodik olarak temizler.
/// </summary>
public sealed class TtsQueueMaintenanceHostedService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaintenanceInterval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan FailedReplayInterval = TimeSpan.FromMinutes(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TtsQueueMaintenanceHostedService> _logger;
    private DateTimeOffset _lastFailedReplayUtc = DateTimeOffset.MinValue;

    public TtsQueueMaintenanceHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<TtsQueueMaintenanceHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken);

        try
        {
            await RunStartupCleanupAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TTS startup cleanup failed.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPeriodicMaintenanceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TTS periodic maintenance failed.");
            }

            await Task.Delay(MaintenanceInterval, stoppingToken);
        }
    }

    private async Task RunStartupCleanupAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var registry = scope.ServiceProvider.GetRequiredService<ITtsAudioRegistry>();

        var result = await registry.ResetAllStuckAsync(includeFailed: true, cancellationToken);
        if (result.QueueReleased > 0 || result.RegistryReset > 0)
        {
            _logger.LogWarning(
                "TTS startup cleanup reset stuck items. Queue released: {QueueReleased}, Registry reset: {RegistryReset}",
                result.QueueReleased,
                result.RegistryReset);
        }
        else
        {
            _logger.LogInformation("TTS startup cleanup found no stuck items.");
        }
    }

    private async Task RunPeriodicMaintenanceAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var registry = scope.ServiceProvider.GetRequiredService<ITtsAudioRegistry>();

        var released = await registry.ReleaseStaleProcessingAsync(cancellationToken);
        if (released > 0)
        {
            _logger.LogWarning("TTS maintenance released {Count} stale processing item(s).", released);
        }

        var now = DateTimeOffset.UtcNow;
        if (now - _lastFailedReplayUtc >= FailedReplayInterval)
        {
            var replayed = await registry.ReplayFailedAsync(200, cancellationToken);
            _lastFailedReplayUtc = now;

            if (replayed > 0)
            {
                _logger.LogInformation("TTS maintenance re-queued {Count} failed item(s).", replayed);
            }
        }
    }
}
