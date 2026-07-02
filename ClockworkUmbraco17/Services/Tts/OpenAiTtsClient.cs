using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;
using Kelimebull.Tts.Core.Models;
using Kelimebull.Tts.Core.Speech;

namespace ClockworkUmbraco.Services.Tts;

public interface IOpenAiTtsClient
{
    Task<OpenAiTtsResult> GenerateAsync(TtsQueueItem item, CancellationToken cancellationToken = default);
}

public sealed class OpenAiTtsClient : IOpenAiTtsClient
{
    private static readonly Uri SpeechUri = new("https://api.openai.com/v1/audio/speech");
    private readonly HttpClient _httpClient;
    private readonly TtsOptions _options;

    public OpenAiTtsClient(HttpClient httpClient, IOptions<TtsOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<OpenAiTtsResult> GenerateAsync(TtsQueueItem item, CancellationToken cancellationToken = default)
    {
        var apiKey = _options.OpenAiApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Set Tts:OpenAiApiKey.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, SpeechUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));

        var body = new Dictionary<string, object?>
        {
            ["model"] = item.Model,
            ["input"] = item.NormalizedText,
            ["voice"] = item.Voice,
            ["response_format"] = item.Format,
        };

        if (IsHeadwordSource(item.SourceType))
        {
            body["speed"] = 0.85;
        }

        var instructions = ResolveInstructions(item);
        if (!string.IsNullOrWhiteSpace(instructions))
        {
            body["instructions"] = instructions;
        }

        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"OpenAI TTS failed with {(int)response.StatusCode}: {error}");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var requestId = response.Headers.TryGetValues("x-request-id", out var values) ? values.FirstOrDefault() : null;
        return new OpenAiTtsResult(bytes, requestId);
    }

    private string? ResolveInstructions(TtsQueueItem item)
    {
        if (!OpenAiTtsModelCapabilities.SupportsInstructions(item.Model))
        {
            return null;
        }

        var languageInstructions = _options.Languages.Values.FirstOrDefault(x =>
            string.Equals(x.Language, item.Language, StringComparison.OrdinalIgnoreCase))?.Instructions;

        if (!IsHeadwordSource(item.SourceType))
        {
            return languageInstructions?.Trim();
        }

        return CombineInstructions(languageInstructions, _options.HeadwordInstructions);
    }

    private static bool IsHeadwordSource(string? sourceType)
    {
        var source = sourceType?.Trim().ToLowerInvariant();
        return source is "word" or "word-of-the-day";
    }

    private static string? CombineInstructions(string? languageInstructions, string? headwordInstructions)
    {
        if (string.IsNullOrWhiteSpace(languageInstructions))
        {
            return string.IsNullOrWhiteSpace(headwordInstructions) ? null : headwordInstructions.Trim();
        }

        if (string.IsNullOrWhiteSpace(headwordInstructions))
        {
            return languageInstructions.Trim();
        }

        return $"{languageInstructions.Trim()} {headwordInstructions.Trim()}";
    }
}

public sealed record OpenAiTtsResult(byte[] AudioBytes, string? RequestId);
