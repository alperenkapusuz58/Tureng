using Kelimebull.Tts.Core.Data;

namespace ClockworkUmbraco.Services.Tts;

public interface ITtsInventoryService
{
    Task<TtsInventoryResult> EnqueueAsync(IEnumerable<TtsInventoryRequest> requests, CancellationToken cancellationToken = default);
}

public sealed class TtsInventoryService : ITtsInventoryService
{
    private readonly ITtsAudioRegistry _registry;
    private readonly ITtsAudioUrlBuilder _urlBuilder;

    public TtsInventoryService(ITtsAudioRegistry registry, ITtsAudioUrlBuilder urlBuilder)
    {
        _registry = registry;
        _urlBuilder = urlBuilder;
    }

    public async Task<TtsInventoryResult> EnqueueAsync(IEnumerable<TtsInventoryRequest> requests, CancellationToken cancellationToken = default)
    {
        var queued = 0;
        var skipped = 0;

        foreach (var request in requests)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                skipped++;
                continue;
            }

            var descriptor = _urlBuilder.CreateDescriptor(request.Text, request.Language, request.SourceType);
            if (descriptor.NormalizedText.Length == 0)
            {
                skipped++;
                continue;
            }

            await _registry.EnsureQueuedAsync(descriptor, cancellationToken);
            queued++;
        }

        return new TtsInventoryResult(queued, skipped);
    }
}

public sealed record TtsInventoryRequest(string Text, string Language, string SourceType);

public sealed record TtsInventoryResult(int Queued, int Skipped);
