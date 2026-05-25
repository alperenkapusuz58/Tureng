using ClockworkUmbraco.Services.Tts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;
using Kelimebull.Tts.Core.Data;
using Kelimebull.Tts.Core.Models;

namespace ClockworkUmbraco.Controllers.Api;

[ApiController]
[Route("api/dictionary/audio")]
public sealed class AudioController : ControllerBase
{
    private readonly ITtsAudioRegistry _registry;
    private readonly ITtsAudioUrlBuilder _urlBuilder;
    private readonly TtsOptions _options;
    private readonly IConfiguration _configuration;

    public AudioController(
        ITtsAudioRegistry registry,
        ITtsAudioUrlBuilder urlBuilder,
        IOptions<TtsOptions> options,
        IConfiguration configuration)
    {
        _registry = registry;
        _urlBuilder = urlBuilder;
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
            var url = !string.IsNullOrWhiteSpace(record.CdnUrl)
                ? record.CdnUrl
                : _urlBuilder.BuildCdnUrl(record.StorageKey ?? descriptor.StorageKey);

            return Ok(new AudioResponseDto("ready", descriptor.ContentHash, url, null));
        }

        return Accepted(new AudioResponseDto("pending", descriptor.ContentHash, null, null));
    }

    [HttpGet("status/{hash}")]
    [Produces("application/json")]
    public async Task<ActionResult<AudioResponseDto>> GetStatus(string hash, CancellationToken cancellationToken = default)
    {
        var record = await _registry.GetByHashAsync(hash, cancellationToken);
        if (record is null)
        {
            return NotFound(new AudioResponseDto("not_found", hash, null, null));
        }

        if (string.Equals(record.Status, TtsStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            var url = !string.IsNullOrWhiteSpace(record.CdnUrl)
                ? record.CdnUrl
                : _urlBuilder.BuildCdnUrl(record.StorageKey ?? string.Empty);

            return Ok(new AudioResponseDto("ready", hash, url, null));
        }

        return Ok(new AudioResponseDto(record.Status, hash, null, record.ErrorMessage));
    }

    [HttpGet("metrics")]
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
}

public sealed record AudioResponseDto(string Status, string? Hash, string? Url, string? Error);
