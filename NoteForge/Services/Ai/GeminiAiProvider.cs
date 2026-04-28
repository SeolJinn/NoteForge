using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NoteForge.Configuration;
using NoteForge.Interfaces;
using NoteForge.Models;
using NoteForge.Services.Ai.Internal;

namespace NoteForge.Services.Ai;

public sealed class GeminiAiProvider : IAiService
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";
    private readonly HttpClient _httpClient;
    private readonly Func<string?> _apiKeyResolver;

    public GeminiAiProvider(HttpClient httpClient, Func<string?>? apiKeyResolver = null)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _apiKeyResolver = apiKeyResolver ?? (() => ApiKeyVault.TryGet(AiProviderType.Gemini));
    }

    public int EmbeddingDimension =>
        AiModelCatalog.FindEmbeddingModel(AiProviderType.Gemini, AiSettings.GeminiEmbeddingModel)?.Dimension ?? 1536;

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var key = RequireKey();
        var url = $"{BaseUrl}/models/{AiSettings.GeminiChatModel}:streamGenerateContent?alt=sse";

        var body = JsonSerializer.Serialize(new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } }
        });

        HttpRequestMessage BuildStreamRequest()
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            msg.Headers.Add("x-goog-api-key", key);
            return msg;
        }

        using var response = await HttpRetry.SendWithRetryAsync(_httpClient, BuildStreamRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessOrThrow(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await foreach (var json in SseParser.ReadEventsAsync(stream, cancellationToken))
        {
            var chunk = JsonSerializer.Deserialize<StreamChunk>(json);
            var text = chunk?.Candidates is { Length: > 0 } c
                ? c[0].Content?.Parts is { Length: > 0 } p ? p[0].Text : null
                : null;
            if (!string.IsNullOrEmpty(text))
            {
                yield return text;
            }
        }
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var key = RequireKey();
        var url = $"{BaseUrl}/models/{AiSettings.GeminiEmbeddingModel}:embedContent";

        var body = JsonSerializer.Serialize(new
        {
            content = new { parts = new[] { new { text } } },
            outputDimensionality = EmbeddingDimension
        });

        HttpRequestMessage BuildEmbedRequest()
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            msg.Headers.Add("x-goog-api-key", key);
            return msg;
        }

        using var response = await HttpRetry.SendWithRetryAsync(_httpClient, BuildEmbedRequest, HttpCompletionOption.ResponseContentRead, cancellationToken);
        await EnsureSuccessOrThrow(response, cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonSerializer.Deserialize<EmbeddingEnvelope>(json);
        return parsed?.Embedding?.Values;
    }

    public async Task<TestConnectionResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var key = _apiKeyResolver();
        if (string.IsNullOrEmpty(key))
        {
            return new TestConnectionResult(false, "API key not set.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/models");
            request.Headers.Add("x-goog-api-key", key);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return new TestConnectionResult(true);
            }
            if ((int)response.StatusCode is 401 or 403)
            {
                return new TestConnectionResult(false, "API key is invalid.");
            }
            return new TestConnectionResult(false, $"Gemini responded with {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return new TestConnectionResult(false, $"Could not reach Gemini: {ex.Message}");
        }
    }

    public void Dispose() => _httpClient.Dispose();

    private string RequireKey()
    {
        var key = _apiKeyResolver();
        if (string.IsNullOrEmpty(key))
        {
            throw new AiAuthException("Gemini", "API key not set.", 401);
        }
        return key;
    }

    private static async Task EnsureSuccessOrThrow(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        var status = (int)response.StatusCode;
        var excerpt = body.Length > 500 ? body[..500] : body;

        if (status is 401 or 403)
        {
            throw new AiAuthException("Gemini", "Gemini rejected the API key.", status, excerpt);
        }
        throw new AiProviderException("Gemini", $"Gemini HTTP {status}.", status, excerpt);
    }

    private sealed class StreamChunk
    {
        [JsonPropertyName("candidates")] public Candidate[]? Candidates { get; set; }
    }

    private sealed class Candidate
    {
        [JsonPropertyName("content")] public ContentBlock? Content { get; set; }
    }

    private sealed class ContentBlock
    {
        [JsonPropertyName("parts")] public Part[]? Parts { get; set; }
    }

    private sealed class Part
    {
        [JsonPropertyName("text")] public string? Text { get; set; }
    }

    private sealed class EmbeddingEnvelope
    {
        [JsonPropertyName("embedding")] public EmbeddingValues? Embedding { get; set; }
    }

    private sealed class EmbeddingValues
    {
        [JsonPropertyName("values")] public float[]? Values { get; set; }
    }
}
