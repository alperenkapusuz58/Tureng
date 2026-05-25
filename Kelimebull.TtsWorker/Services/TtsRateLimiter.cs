using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;

namespace Kelimebull.TtsWorker.Services;

public sealed class TtsRateLimiter
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeSpan _minimumDelay;
    private DateTimeOffset _lastRequestUtc = DateTimeOffset.MinValue;

    public TtsRateLimiter(IOptions<TtsOptions> options)
    {
        var requestsPerMinute = Math.Max(1, options.Value.RequestsPerMinute);
        _minimumDelay = TimeSpan.FromMinutes(1) / requestsPerMinute;
    }

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var elapsed = DateTimeOffset.UtcNow - _lastRequestUtc;
            if (elapsed < _minimumDelay)
            {
                await Task.Delay(_minimumDelay - elapsed, cancellationToken);
            }

            _lastRequestUtc = DateTimeOffset.UtcNow;
        }
        finally
        {
            _gate.Release();
        }
    }
}
