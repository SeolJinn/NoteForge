using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NoteForge.Configuration;
using NoteForge.Interfaces;
using NoteForge.Models;
using NoteForge.Services.Ai;
using NoteForge.Services.Embeddings;

namespace NoteForge.Tests.Services;

[Collection("Settings")]
public class EmbeddingServiceTests : IAsyncLifetime
{
    private string _dbPath = string.Empty;
    private EmbeddingRepository _repo = null!;
    private FakeAiService _ai = null!;
    private EmbeddingService _service = null!;
    private TimeSpan _originalDebounce;

    public async Task InitializeAsync()
    {
        _originalDebounce = EmbeddingService.UpdateDebounce;
        EmbeddingService.UpdateDebounce = TimeSpan.FromMilliseconds(50);

        _dbPath = Path.Combine(Path.GetTempPath(), $"NoteForgeESvc_{Guid.NewGuid():N}.db");
        _repo = new EmbeddingRepository();
        await _repo.InitializeAsync(_dbPath);

        _ai = new FakeAiService();
        _service = new EmbeddingService(_ai, _repo, NullLogger<EmbeddingService>.Instance);

        AiSettings.ActiveProvider = AiProviderType.Ollama;
    }

    public Task DisposeAsync()
    {
        EmbeddingService.UpdateDebounce = _originalDebounce;
        _repo.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task QueueEmbeddingUpdate_skips_when_content_hash_unchanged()
    {
        var note = new Note { FilePath = "/a.md", Filename = "a", Text = "this is the content of the note" };
        await _service.GenerateEmbeddingForNoteAsync(note);
        var initialCallCount = _ai.GenerateCallCount;

        _service.QueueEmbeddingUpdate(note);
        await WaitForDebouncedTask();

        Assert.Equal(initialCallCount, _ai.GenerateCallCount);
    }

    [Fact]
    public async Task QueueEmbeddingUpdate_generates_when_content_changed()
    {
        var note = new Note { FilePath = "/a.md", Filename = "a", Text = "original content here" };
        await _service.GenerateEmbeddingForNoteAsync(note);
        var initialCallCount = _ai.GenerateCallCount;

        note.Text = "completely different content now";
        _service.QueueEmbeddingUpdate(note);
        await WaitForDebouncedTask();

        Assert.Equal(initialCallCount + 1, _ai.GenerateCallCount);
    }

    [Fact]
    public async Task QueueEmbeddingUpdate_coalesces_rapid_calls_into_one_generation()
    {
        EmbeddingService.UpdateDebounce = TimeSpan.FromMilliseconds(150);

        var note = new Note { FilePath = "/a.md", Filename = "a", Text = "v1 content of note" };
        var initialCallCount = _ai.GenerateCallCount;

        for (int i = 0; i < 5; i++)
        {
            note.Text = $"v{i} content of note here";
            _service.QueueEmbeddingUpdate(note);
            await Task.Delay(20);
        }
        await Task.Delay(400);

        Assert.Equal(initialCallCount + 1, _ai.GenerateCallCount);
    }

    [Fact]
    public async Task QueueEmbeddingUpdate_with_oldPath_deletes_old_record()
    {
        var oldPath = "/old.md";
        var newPath = "/new.md";
        await _repo.SaveEmbeddingAsync(oldPath, [0.1f, 0.2f], "h");

        var note = new Note { FilePath = newPath, Filename = "new", Text = "content for the note" };
        _service.QueueEmbeddingUpdate(note, oldPathToDelete: oldPath);
        await WaitForDebouncedTask();

        Assert.Null(await _repo.GetEmbeddingAsync(oldPath));
    }

    [Fact]
    public async Task QueueEmbeddingUpdate_skips_when_text_too_short()
    {
        var note = new Note { FilePath = "/a.md", Filename = "a", Text = "tiny" };
        var initialCallCount = _ai.GenerateCallCount;

        _service.QueueEmbeddingUpdate(note);
        await WaitForDebouncedTask();

        Assert.Equal(initialCallCount, _ai.GenerateCallCount);
    }

    [Fact]
    public async Task RegenerateAllAsync_clears_then_repopulates_with_metadata()
    {
        await _repo.SaveEmbeddingAsync("/old.md", [0.9f], "stale-hash");
        await _repo.SetMetadataAsync("Ollama", 768, "old-model");
        AiSettings.OllamaEmbeddingModel = "nomic-embed-text";

        var notes = new[]
        {
            new Note { FilePath = "/n1.md", Filename = "n1", Text = "content of note one here" },
            new Note { FilePath = "/n2.md", Filename = "n2", Text = "content of note two here" }
        };

        await _service.RegenerateAllAsync(notes);
        await Task.Delay(200);

        var all = await _repo.GetAllEmbeddingsAsync();
        Assert.DoesNotContain(all, e => e.FilePath == "/old.md");

        var metadata = await _repo.GetMetadataAsync();
        Assert.NotNull(metadata);
        Assert.Equal("Ollama", metadata.ProviderName);
        Assert.Equal("nomic-embed-text", metadata.ModelId);
    }

    [Fact]
    public async Task EnsureEmbeddingsAsync_regenerates_when_stored_provider_differs()
    {
        await _repo.SaveEmbeddingAsync("/old.md", [0.9f], "stale-hash");
        await _repo.SetMetadataAsync("OpenAi", 1536, "text-embedding-3-small");
        AiSettings.ActiveProvider = AiProviderType.Ollama;
        AiSettings.OllamaEmbeddingModel = "nomic-embed-text";

        var notes = new[]
        {
            new Note { FilePath = "/n1.md", Filename = "n1", Text = "content of note one here" }
        };

        await _service.EnsureEmbeddingsAsync(notes);
        await Task.Delay(200);

        var all = await _repo.GetAllEmbeddingsAsync();
        Assert.DoesNotContain(all, e => e.FilePath == "/old.md");
        Assert.Contains(all, e => e.FilePath == "/n1.md");

        var metadata = await _repo.GetMetadataAsync();
        Assert.NotNull(metadata);
        Assert.Equal("Ollama", metadata.ProviderName);
        Assert.Equal(768, metadata.Dimension);
    }

    [Fact]
    public async Task EnsureEmbeddingsAsync_keeps_existing_when_provider_matches()
    {
        AiSettings.ActiveProvider = AiProviderType.Ollama;
        AiSettings.OllamaEmbeddingModel = "nomic-embed-text";

        var note = new Note { FilePath = "/n1.md", Filename = "n1", Text = "content of note one here" };
        await _service.GenerateEmbeddingForNoteAsync(note);
        await _repo.SetMetadataAsync("Ollama", 768, "nomic-embed-text");
        var callsAfterSeed = _ai.GenerateCallCount;

        await _service.EnsureEmbeddingsAsync([note]);
        await Task.Delay(200);

        Assert.Equal(callsAfterSeed, _ai.GenerateCallCount);
    }

    private static async Task WaitForDebouncedTask()
    {
        await Task.Delay(EmbeddingService.UpdateDebounce + TimeSpan.FromMilliseconds(150));
    }

    private sealed class FakeAiService : IAiService
    {
        public int GenerateCallCount { get; private set; }
        public int EmbeddingDimension => 768;

        public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            GenerateCallCount++;
            return Task.FromResult<float[]?>([0.1f, 0.2f, 0.3f]);
        }

        public IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<TestConnectionResult> TestConnectionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new TestConnectionResult(true));

        public void Dispose() { }
    }
}
