using System.Text.RegularExpressions;
using ClockworkUmbraco.Composers;
using ClockworkUmbraco.Services.Tts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;
using Kelimebull.Tts.Core.Data;
using Kelimebull.Tts.Core.Models;

namespace ClockworkUmbraco.Controllers.Api;

[ApiController]
[Route("api/dictionary/audio")]
[EnableRateLimiting(RegisterServiceComposer.TtsAudioRateLimitPolicy)]
public sealed partial class AudioController : ControllerBase
{
    private readonly ITtsAudioRegistry _registry;
    private readonly ITtsAudioUrlBuilder _urlBuilder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TtsDispatchService _dispatchService;
    private readonly TtsOptions _options;
    private readonly IConfiguration _configuration;

    public AudioController(
        ITtsAudioRegistry registry,
        ITtsAudioUrlBuilder urlBuilder,
        IHttpClientFactory httpClientFactory,
        TtsDispatchService dispatchService,
        IOptions<TtsOptions> options,
        IConfiguration configuration)
    {
        _registry = registry;
        _urlBuilder = urlBuilder;
        _httpClientFactory = httpClientFactory;
        _dispatchService = dispatchService;
        _options = options.Value;
        _configuration = configuration;
    }

    [HttpGet]
    [Produces("application/json")]
    public async Task<ActionResult<AudioResponseDto>> GetAudio(
        [FromQuery] string? text,
        [FromQuery] string? language = "en-US",
        [FromQuery] string? sourceType = "word",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest(new AudioResponseDto("invalid", null, null, "Text is required."));
        }

        var descriptor = _urlBuilder.CreateDescriptor(text, language, sourceType);
        if (descriptor.NormalizedText.Length == 0)
        {
            return BadRequest(new AudioResponseDto("invalid", descriptor.ContentHash, null, "Text is empty after normalization."));
        }

        if (descriptor.NormalizedText.Length > _options.MaxTextLength)
        {
            return BadRequest(new AudioResponseDto("invalid", descriptor.ContentHash, null, $"Text exceeds max length of {_options.MaxTextLength}."));
        }

        var record = await _registry.EnsureQueuedAsync(descriptor, cancellationToken);
        if (string.Equals(record.Status, TtsStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new AudioResponseDto("ready", descriptor.ContentHash, _urlBuilder.BuildStreamUrl(descriptor.ContentHash), null));
        }

        _dispatchService.TryProcessInBackground(descriptor.ContentHash);
        return Accepted(new AudioResponseDto("pending", descriptor.ContentHash, null, null));
    }

    [HttpGet("status/{hash}")]
    [Produces("application/json")]
    public async Task<ActionResult<AudioResponseDto>> GetStatus(string hash, CancellationToken cancellationToken = default)
    {
        if (!IsValidHash(hash))
        {
            return BadRequest(new AudioResponseDto("invalid", hash, null, "Invalid hash."));
        }

        var snapshot = await _registry.GetStatusSnapshotAsync(hash, cancellationToken);
        if (snapshot is null)
        {
            return NotFound(new AudioResponseDto("not_found", hash, null, null));
        }

        var status = TtsStatusResolver.ResolveStatus(snapshot);
        if (string.Equals(status, "ready", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new AudioResponseDto("ready", hash, _urlBuilder.BuildStreamUrl(hash), null));
        }

        if (string.Equals(status, TtsStatuses.Pending, StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, TtsStatuses.Processing, StringComparison.OrdinalIgnoreCase))
        {
            await _registry.ReleaseAbandonedProcessingAsync(hash, cancellationToken);
            snapshot = await _registry.GetStatusSnapshotAsync(hash, cancellationToken)
                ?? snapshot;
            status = TtsStatusResolver.ResolveStatus(snapshot);

            if (string.Equals(status, TtsStatuses.Pending, StringComparison.OrdinalIgnoreCase)
                || string.Equals(status, TtsStatuses.Processing, StringComparison.OrdinalIgnoreCase))
            {
                _dispatchService.TryProcessInBackground(hash);
            }
        }

        if (string.Equals(status, "ready", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new AudioResponseDto("ready", hash, _urlBuilder.BuildStreamUrl(hash), null));
        }

        return Ok(new AudioResponseDto(status, hash, null, TtsStatusResolver.ResolveError(snapshot)));
    }

    [HttpGet("stream/{hash}")]
    [ResponseCache(Duration = 31536000, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> StreamAudio(string hash, CancellationToken cancellationToken = default)
    {
        if (!IsValidHash(hash))
        {
            return BadRequest();
        }

        var record = await _registry.GetByHashAsync(hash, cancellationToken);
        if (record is null || !string.Equals(record.Status, TtsStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var cdnUrl = !string.IsNullOrWhiteSpace(record.CdnUrl)
            ? record.CdnUrl
            : _urlBuilder.BuildCdnUrl(record.StorageKey ?? string.Empty);

        if (string.IsNullOrWhiteSpace(cdnUrl) || !Uri.TryCreate(cdnUrl, UriKind.Absolute, out _))
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }

        var client = _httpClientFactory.CreateClient("TtsAudioStream");
        using var response = await client.GetAsync(cdnUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "audio/mpeg";
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        Response.Headers.CacheControl = "public, max-age=31536000, immutable";

        return new FileContentResult(bytes, contentType)
        {
            EnableRangeProcessing = true,
        };
    }

    [HttpGet("metrics")]
    [DisableRateLimiting]
    [Produces("application/json")]
    public async Task<ActionResult<TtsUsageSummary>> Metrics(CancellationToken cancellationToken = default)
    {
        if (!IsAdminRequest())
        {
            return Unauthorized();
        }

        var sinceUtc = DateTimeOffset.UtcNow.Date;
        return Ok(await _registry.GetUsageSummaryAsync(sinceUtc, cancellationToken));
    }

    [HttpPost("replay-failed")]
    [DisableRateLimiting]
    [Produces("application/json")]
    public async Task<ActionResult<object>> ReplayFailed([FromQuery] int maxItems = 100, CancellationToken cancellationToken = default)
    {
        if (!IsAdminRequest())
        {
            return Unauthorized();
        }

        var count = await _registry.ReplayFailedAsync(maxItems, cancellationToken);
        return Ok(new { replayed = count });
    }

    [HttpPost("reset-stuck")]
    [DisableRateLimiting]
    [Produces("application/json")]
    public async Task<ActionResult<TtsBulkResetResult>> ResetStuck(
        [FromQuery] bool includeFailed = false,
        CancellationToken cancellationToken = default)
    {
        if (!IsAdminRequest())
        {
            return Unauthorized();
        }

        return Ok(await _registry.ResetAllStuckAsync(includeFailed, cancellationToken));
    }

    private static bool IsValidHash(string? hash)
        => !string.IsNullOrWhiteSpace(hash) && ContentHashRegex().IsMatch(hash);

    private bool IsAdminRequest()
    {
        var expected = _configuration["TTS_ADMIN_KEY"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            expected = _configuration["Tts:AdminKey"];
        }

        if (string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        var provided = Request.Headers["X-TTS-Admin-Key"].FirstOrDefault();
        return string.Equals(provided, expected, StringComparison.Ordinal);
    }

    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.IgnoreCase)]
    private static partial Regex ContentHashRegex();
}

public sealed record AudioResponseDto(string Status, string? Hash, string? Url, string? Error);
