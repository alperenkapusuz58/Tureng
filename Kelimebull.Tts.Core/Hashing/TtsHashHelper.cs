using System.Security.Cryptography;
using System.Text;

namespace Kelimebull.Tts.Core.Hashing;

public static class TtsHashHelper
{
    public static string CreateHash(string normalizedText, string language, string voice, string model, string format, string pipelineVersion)
    {
        var payload = string.Join(
            "|",
            pipelineVersion.Trim().ToLowerInvariant(),
            language.Trim().ToLowerInvariant(),
            voice.Trim().ToLowerInvariant(),
            model.Trim().ToLowerInvariant(),
            format.Trim().ToLowerInvariant(),
            normalizedText);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string BuildStorageKey(string pipelineVersion, string contentHash, string format)
    {
        var version = string.IsNullOrWhiteSpace(pipelineVersion) ? "v1" : pipelineVersion.Trim().Trim('/');
        var extension = string.IsNullOrWhiteSpace(format) ? "mp3" : format.Trim().TrimStart('.');
        return $"audio/{version}/{contentHash}.{extension}";
    }
}
