using Kelimebull.Tts.Core.Models;

namespace ClockworkUmbraco.Services.Tts;

public interface ITtsAudioUrlBuilder
{
    TtsAudioDescriptor CreateDescriptor(string text, string? language, string? sourceType);

    string BuildCdnUrl(string storageKey);

    string BuildStreamUrl(string contentHash);
}
