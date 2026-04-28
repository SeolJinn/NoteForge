using NoteForge.Configuration;
using NoteForge.Interfaces;
using NoteForge.Services.Ai;

namespace NoteForge.Tests;

[Collection("Settings")]
public class AiProviderRegistryTests
{
    [Fact]
    public void Returns_correct_provider_for_active_setting()
    {
        var disabled = new StubProvider("disabled", 0);
        var ollama = new StubProvider("ollama", 768);
        var openai = new StubProvider("openai", 1536);
        var gemini = new StubProvider("gemini", 1536);

        var registry = new AiProviderRegistry(disabled, ollama, openai, gemini);

        AiSettings.ActiveProvider = AiProviderType.Disabled;
        Assert.Same(disabled, registry.Current);

        AiSettings.ActiveProvider = AiProviderType.Ollama;
        Assert.Same(ollama, registry.Current);

        AiSettings.ActiveProvider = AiProviderType.OpenAi;
        Assert.Same(openai, registry.Current);

        AiSettings.ActiveProvider = AiProviderType.Gemini;
        Assert.Same(gemini, registry.Current);
    }

    [Fact]
    public void Forwards_EmbeddingDimension_to_active_provider()
    {
        var disabled = new StubProvider("disabled", 0);
        var ollama = new StubProvider("ollama", 768);
        var registry = new AiProviderRegistry(disabled, ollama, ollama, ollama);

        AiSettings.ActiveProvider = AiProviderType.Ollama;
        Assert.Equal(768, registry.EmbeddingDimension);

        AiSettings.ActiveProvider = AiProviderType.Disabled;
        Assert.Equal(0, registry.EmbeddingDimension);
    }

    private sealed class StubProvider(string name, int dim) : IAiService
    {
        public string Name { get; } = name;
        public int EmbeddingDimension => dim;
        public IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<TestConnectionResult> TestConnectionAsync(CancellationToken ct = default) => Task.FromResult(new TestConnectionResult(true));
        public void Dispose() { }
    }
}

[CollectionDefinition("Settings", DisableParallelization = true)]
public class SettingsCollection { }
