using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NoteForge.Configuration;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Services.Ai;

public sealed class OllamaAiProvider : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaAiProvider> _logger;

    public OllamaAiProvider(ILogger<OllamaAiProvider> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    }

    public int EmbeddingDimension =>
        AiModelCatalog.FindEmbeddingModel(AiProviderType.Ollama, AiSettings.OllamaEmbeddingModel)?.Dimension ?? 768;

    public async IAsyncEnumerable<string> StreamCompletionAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            model = AiSettings.OllamaChatModel,
            prompt,
            stream = true
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{AiSettings.OllamaUrl}/api/generate")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaStreamResponse>(line);
            if (chunk?.Response is not null)
            {
                yield return chunk.Response;
            }
            if (chunk?.Done is true)
            {
                yield break;
            }
        }
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var requestBody = new { model = AiSettings.OllamaEmbeddingModel, prompt = text };
        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync($"{AiSettings.OllamaUrl}/api/embeddings", content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var embeddingResponse = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseJson);
            return embeddingResponse?.Embedding;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to generate embedding from Ollama");
            return null;
        }
    }

    public async Task<TestConnectionResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{AiSettings.OllamaUrl}/api/tags", cancellationToken);
            return response.IsSuccessStatusCode
                ? new TestConnectionResult(true)
                : new TestConnectionResult(false, $"Ollama responded with {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return new TestConnectionResult(false, $"Could not reach Ollama: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private sealed class OllamaStreamResponse
    {
        [JsonPropertyName("response")] public string? Response { get; set; }
        [JsonPropertyName("done")] public bool Done { get; set; }
    }

    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")] public float[]? Embedding { get; set; }
    }
}
