using ClockworkUmbraco.Services.Interfaces;
using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;

namespace ClockworkUmbraco.Services.Tts;

public sealed class TtsWarmupHostedService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TtsWarmupHostedService> _logger;

    public TtsWarmupHostedService(IServiceScopeFactory scopeFactory, ILogger<TtsWarmupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(StartupDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await WarmupWordOfTheDayAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TTS warmup failed.");
            }

            await Task.Delay(RefreshInterval, stoppingToken);
        }
    }

    private async Task WarmupWordOfTheDayAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<TtsOptions>>().Value;
        if (string.IsNullOrWhiteSpace(options.OpenAiApiKey))
        {
            return;
        }

        var wordOfTheDay = scope.ServiceProvider.GetRequiredService<IWordOfTheDayService>().GetWordOfTheDay();
        if (wordOfTheDay is null || string.IsNullOrWhiteSpace(wordOfTheDay.Word))
        {
            return;
        }

        var inventory = scope.ServiceProvider.GetRequiredService<ITtsInventoryService>();
        var result = await inventory.EnqueueAsync(
            [new TtsInventoryRequest(wordOfTheDay.Word.Trim(), "en-US", "word-of-the-day")],
            cancellationToken);

        if (result.Queued > 0)
        {
            _logger.LogInformation("TTS warmup queued word of the day: {Word}", wordOfTheDay.Word);
        }
    }
}
