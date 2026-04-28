using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NoteForge.Configuration;
using NoteForge.Interfaces;

namespace NoteForge.Services.Ai;

public sealed class AiProviderRegistry : IAiService
{
    private readonly IAiService _disabled;
    private readonly IAiService _ollama;
    private readonly IAiService _openAi;
    private readonly IAiService _gemini;

    public AiProviderRegistry(
        DisabledAiProvider disabled,
        OllamaAiProvider ollama,
        OpenAiAiProvider openAi,
        GeminiAiProvider gemini)
        : this((IAiService)disabled, ollama, openAi, gemini) { }

    internal AiProviderRegistry(IAiService disabled, IAiService ollama, IAiService openAi, IAiService gemini)
    {
        _disabled = disabled;
        _ollama = ollama;
        _openAi = openAi;
        _gemini = gemini;
    }

    public IAiService Current => AiSettings.ActiveProvider switch
    {
        AiProviderType.Ollama => _ollama,
        AiProviderType.OpenAi => _openAi,
        AiProviderType.Gemini => _gemini,
        _ => _disabled
    };

    public IAiService For(AiProviderType type) => type switch
    {
        AiProviderType.Ollama => _ollama,
        AiProviderType.OpenAi => _openAi,
        AiProviderType.Gemini => _gemini,
        _ => _disabled
    };

    public int EmbeddingDimension => Current.EmbeddingDimension;

    public IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken cancellationToken = default)
        => Current.StreamCompletionAsync(prompt, cancellationToken);

    public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        => Current.GenerateEmbeddingAsync(text, cancellationToken);

    public Task<TestConnectionResult> TestConnectionAsync(CancellationToken cancellationToken = default)
        => Current.TestConnectionAsync(cancellationToken);

    public void Dispose()
    {
        _disabled.Dispose();
        _ollama.Dispose();
        _openAi.Dispose();
        _gemini.Dispose();
    }
}
