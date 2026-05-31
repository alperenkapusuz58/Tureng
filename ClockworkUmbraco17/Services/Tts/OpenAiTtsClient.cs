using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Kelimebull.Tts.Core.Configuration;
using Kelimebull.Tts.Core.Models;

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
    private readonly IConfiguration _configuration;

    public OpenAiTtsClient(IOptions<TtsOptions> options, IConfiguration configuration)
    {
        _options = options.Value;
        _configuration = configuration;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
    }

    public async Task<OpenAiTtsResult> GenerateAsync(TtsQueueItem item, CancellationToken cancellationToken = default)
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is not configured. Set OPENAI_API_KEY or Tts:OpenAiApiKey.");
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

        var instructions = _options.Languages.Values.FirstOrDefault(x =>
            string.Equals(x.Language, item.Language, StringComparison.OrdinalIgnoreCase))?.Instructions;
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

    private string? ResolveApiKey()
    {
        var env = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        return !string.IsNullOrWhiteSpace(env) ? env : _options.OpenAiApiKey;
    }
}

public sealed record OpenAiTtsResult(byte[] AudioBytes, string? RequestId);
