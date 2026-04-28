using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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

public sealed class OpenAiAiProvider : IAiService
{
    private const string BaseUrl = "https://api.openai.com/v1";
    private readonly HttpClient _httpClient;
    private readonly Func<string?> _apiKeyResolver;

    public OpenAiAiProvider(HttpClient httpClient, Func<string?>? apiKeyResolver = null)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _apiKeyResolver = apiKeyResolver ?? (() => ApiKeyVault.TryGet(AiProviderType.OpenAi));
    }

    public int EmbeddingDimension =>
        AiModelCatalog.FindEmbeddingModel(AiProviderType.OpenAi, AiSettings.OpenAiEmbeddingModel)?.Dimension ?? 1536;

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var key = RequireKey();

        var body = JsonSerializer.Serialize(new
        {
            model = AiSettings.OpenAiChatModel,
            messages = new[] { new { role = "user", content = prompt } },
            stream = true
        });

        HttpRequestMessage BuildRequest()
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/chat/completions")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            return msg;
        }

        using var response = await HttpRetry.SendWithRetryAsync(_httpClient, BuildRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await EnsureSuccessOrThrow(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await foreach (var json in SseParser.ReadEventsAsync(stream, cancellationToken))
        {
            var chunk = JsonSerializer.Deserialize<ChatChunk>(json);
            var delta = chunk?.Choices is { Length: > 0 } c ? c[0].Delta?.Content : null;
            if (!string.IsNullOrEmpty(delta))
            {
                yield return delta;
            }
        }
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var key = RequireKey();

        var body = JsonSerializer.Serialize(new
        {
            model = AiSettings.OpenAiEmbeddingModel,
            input = text
        });

        HttpRequestMessage BuildRequest()
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/embeddings")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            return msg;
        }

        using var response = await HttpRetry.SendWithRetryAsync(_httpClient, BuildRequest, HttpCompletionOption.ResponseContentRead, cancellationToken);
        await EnsureSuccessOrThrow(response, cancellationToken);

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonSerializer.Deserialize<EmbeddingResponse>(json);
        return parsed?.Data is { Length: > 0 } d ? d[0].Embedding : null;
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
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new TestConnectionResult(true);
            }
            if ((int)response.StatusCode is 401 or 403)
            {
                return new TestConnectionResult(false, "API key is invalid.");
            }
            return new TestConnectionResult(false, $"OpenAI responded with {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return new TestConnectionResult(false, $"Could not reach OpenAI: {ex.Message}");
        }
    }

    public void Dispose() => _httpClient.Dispose();

    private string RequireKey()
    {
        var key = _apiKeyResolver();
        if (string.IsNullOrEmpty(key))
        {
            throw new AiAuthException("OpenAI", "API key not set.", 401);
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
            throw new AiAuthException("OpenAI", "OpenAI rejected the API key.", status, excerpt);
        }
        throw new AiProviderException("OpenAI", $"OpenAI HTTP {status}.", status, excerpt);
    }

    private sealed class ChatChunk
    {
        [JsonPropertyName("choices")] public ChatChoice[]? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("delta")] public ChatDelta? Delta { get; set; }
    }

    private sealed class ChatDelta
    {
        [JsonPropertyName("content")] public string? Content { get; set; }
    }

    private sealed class EmbeddingResponse
    {
        [JsonPropertyName("data")] public EmbeddingData[]? Data { get; set; }
    }

    private sealed class EmbeddingData
    {
        [JsonPropertyName("embedding")] public float[]? Embedding { get; set; }
    }
}
