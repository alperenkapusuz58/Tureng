using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;

namespace Kelimebull.Tts.Core.Voices;

public sealed class TtsVoiceResolver
{
    private readonly TtsOptions _options;

    public TtsVoiceResolver(IOptions<TtsOptions> options)
    {
        _options = options.Value;
    }

    public TtsVoiceProfile Resolve(string? language)
    {
        var normalizedLanguage = NormalizeLanguage(language);
        var options = _options.Languages.Values.FirstOrDefault(x =>
            string.Equals(NormalizeLanguage(x.Language), normalizedLanguage, StringComparison.OrdinalIgnoreCase));

        options ??= normalizedLanguage.StartsWith("tr", StringComparison.OrdinalIgnoreCase)
            ? FindByKey("Turkish")
            : FindByKey("English");

        return new TtsVoiceProfile(
            options?.Language ?? normalizedLanguage,
            string.IsNullOrWhiteSpace(options?.Voice) ? "nova" : options.Voice,
            string.IsNullOrWhiteSpace(options?.Model) ? _options.DefaultModel : options.Model,
            string.IsNullOrWhiteSpace(_options.DefaultFormat) ? "mp3" : _options.DefaultFormat,
            options?.Instructions);
    }

    private TtsLanguageOptions? FindByKey(string key)
    {
        return _options.Languages.TryGetValue(key, out var options) ? options : null;
    }

    private static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en-US";
        }

        var value = language.Trim();
        return value.Equals("tr", StringComparison.OrdinalIgnoreCase) ? "tr-TR" : value;
    }
}

public sealed record TtsVoiceProfile(string Language, string Voice, string Model, string Format, string? Instructions);
