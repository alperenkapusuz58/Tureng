using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;
using Kelimebull.Tts.Core.Hashing;
using Kelimebull.Tts.Core.Models;
using Kelimebull.Tts.Core.Voices;

namespace ClockworkUmbraco.Services.Tts;

public sealed class TtsAudioUrlBuilder : ITtsAudioUrlBuilder
{
    private readonly TtsOptions _options;
    private readonly TtsVoiceResolver _voiceResolver;

    public TtsAudioUrlBuilder(IOptions<TtsOptions> options, TtsVoiceResolver voiceResolver)
    {
        _options = options.Value;
        _voiceResolver = voiceResolver;
    }

    public TtsAudioDescriptor CreateDescriptor(string text, string? language, string? sourceType)
    {
        var normalizedText = TtsTextNormalizer.Normalize(text);
        var profile = _voiceResolver.Resolve(language);
        var pipelineVersion = string.IsNullOrWhiteSpace(_options.PipelineVersion) ? "v1" : _options.PipelineVersion;
        var contentHash = TtsHashHelper.CreateHash(
            normalizedText,
            profile.Language,
            profile.Voice,
            profile.Model,
            profile.Format,
            pipelineVersion);
        var storageKey = TtsHashHelper.BuildStorageKey(pipelineVersion, contentHash, profile.Format);

        return new TtsAudioDescriptor(
            contentHash,
            text.Trim(),
            normalizedText,
            profile.Language,
            profile.Voice,
            profile.Model,
            profile.Format,
            pipelineVersion,
            string.IsNullOrWhiteSpace(sourceType) ? "unknown" : sourceType.Trim().ToLowerInvariant(),
            normalizedText.Length,
            storageKey);
    }

    public string BuildCdnUrl(string storageKey)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(_options.CdnBaseUrl)
            ? _options.CdnBaseUrl
            : _options.R2.PublicBaseUrl;

        return $"{baseUrl.TrimEnd('/')}/{storageKey.TrimStart('/')}";
    }
}
