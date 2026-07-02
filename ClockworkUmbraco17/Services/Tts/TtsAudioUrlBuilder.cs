using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;
using Kelimebull.Tts.Core.Hashing;
using Kelimebull.Tts.Core.Models;
using Kelimebull.Tts.Core.Speech;
using Kelimebull.Tts.Core.Voices;

namespace ClockworkUmbraco.Services.Tts;

public sealed class TtsAudioUrlBuilder : ITtsAudioUrlBuilder
{
    private readonly TtsOptions _options;
    private readonly TtsVoiceResolver _voiceResolver;
    private readonly ITtsHeadwordSpeechInputBuilder _headwordSpeechInputBuilder;

    public TtsAudioUrlBuilder(
        IOptions<TtsOptions> options,
        TtsVoiceResolver voiceResolver,
        ITtsHeadwordSpeechInputBuilder headwordSpeechInputBuilder)
    {
        _options = options.Value;
        _voiceResolver = voiceResolver;
        _headwordSpeechInputBuilder = headwordSpeechInputBuilder;
    }

    public TtsAudioDescriptor CreateDescriptor(string text, string? language, string? sourceType)
    {
        var normalizedText = TtsTextNormalizer.Normalize(text);
        var normalizedSourceType = string.IsNullOrWhiteSpace(sourceType) ? "unknown" : sourceType.Trim().ToLowerInvariant();
        var resolvedLanguage = TtsSpeechLanguageResolver.Resolve(language, normalizedText, normalizedSourceType);
        var speechInput = _headwordSpeechInputBuilder.Build(normalizedText, resolvedLanguage, normalizedSourceType);
        var profile = _voiceResolver.Resolve(resolvedLanguage);
        var pipelineVersion = string.IsNullOrWhiteSpace(_options.PipelineVersion) ? "v1" : _options.PipelineVersion;
        var contentHash = TtsHashHelper.CreateHash(
            speechInput,
            profile.Language,
            profile.Voice,
            profile.Model,
            profile.Format,
            pipelineVersion);
        var storageKey = TtsHashHelper.BuildStorageKey(pipelineVersion, contentHash, profile.Format);

        return new TtsAudioDescriptor(
            contentHash,
            text.Trim(),
            speechInput,
            profile.Language,
            profile.Voice,
            profile.Model,
            profile.Format,
            pipelineVersion,
            normalizedSourceType,
            speechInput.Length,
            storageKey);
    }

    public string BuildCdnUrl(string storageKey)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(_options.CdnBaseUrl)
            ? _options.CdnBaseUrl
            : _options.R2.PublicBaseUrl;

        return $"{baseUrl.TrimEnd('/')}/{storageKey.TrimStart('/')}";
    }

    public string BuildStreamUrl(string contentHash)
        => $"/api/dictionary/audio/stream/{contentHash}";
}
