using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;
using Kelimebull.Tts.Core.Data;

namespace ClockworkUmbraco.Services.Tts;

public sealed class TtsGenerationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TtsOptions _options;
    private readonly ILogger<TtsGenerationWorker> _logger;
    private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    public TtsGenerationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<TtsOptions> options,
        ILogger<TtsGenerationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TTS generation worker started with id {WorkerId}", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var registry = scope.ServiceProvider.GetRequiredService<ITtsAudioRegistry>();
                var processor = scope.ServiceProvider.GetRequiredService<TtsGenerationProcessor>();

                var items = await registry.ClaimPendingAsync(
                    _workerId,
                    _options.WorkerBatchSize,
                    TimeSpan.FromMinutes(5),
                    stoppingToken);

                if (items.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                    continue;
                }

                var parallelism = Math.Max(1, _options.WorkerParallelism);
                await Parallel.ForEachAsync(
                    items,
                    new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = stoppingToken },
                    async (item, token) => await processor.ProcessAsync(item, token));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TTS generation worker iteration failed. Retrying after delay.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
