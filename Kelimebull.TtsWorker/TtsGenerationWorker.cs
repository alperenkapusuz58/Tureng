using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;
using Kelimebull.Tts.Core.Data;
using Kelimebull.TtsWorker.Services;

namespace Kelimebull.TtsWorker;

public sealed class TtsGenerationWorker : BackgroundService
{
    private readonly ITtsAudioRegistry _registry;
    private readonly TtsGenerationProcessor _processor;
    private readonly TtsOptions _options;
    private readonly ILogger<TtsGenerationWorker> _logger;
    private readonly string _workerId = $"{Environment.MachineName}-{Guid.NewGuid():N}";

    public TtsGenerationWorker(
        ITtsAudioRegistry registry,
        TtsGenerationProcessor processor,
        IOptions<TtsOptions> options,
        ILogger<TtsGenerationWorker> logger)
    {
        _registry = registry;
        _processor = processor;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TTS generation worker started with id {WorkerId}", _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            var items = await _registry.ClaimPendingAsync(
                _workerId,
                _options.WorkerBatchSize,
                TimeSpan.FromMinutes(5),
                stoppingToken);

            if (items.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                continue;
            }

            var parallelism = Math.Max(1, _options.WorkerParallelism);
            await Parallel.ForEachAsync(
                items,
                new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = stoppingToken },
                async (item, token) => await _processor.ProcessAsync(item, token));
        }
    }
}
