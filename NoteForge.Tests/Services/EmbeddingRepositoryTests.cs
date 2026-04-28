using NoteForge.Interfaces;
using NoteForge.Services.Embeddings;

namespace NoteForge.Tests.Services;

public class EmbeddingRepositoryTests : IAsyncLifetime
{
    private string _dbPath = string.Empty;
    private EmbeddingRepository _repo = null!;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"NoteForgeTest_{Guid.NewGuid():N}.db");
        _repo = new EmbeddingRepository();
        await _repo.InitializeAsync(_dbPath);
    }

    public Task DisposeAsync()
    {
        _repo.Dispose();
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SaveAndGet_round_trips_embedding()
    {
        var path = "/vault/note.md";
        var embedding = new[] { 0.1f, 0.2f, 0.3f };

        var saved = await _repo.SaveEmbeddingAsync(path, embedding, "hash1");

        Assert.True(saved);
        var record = await _repo.GetEmbeddingAsync(path);
        Assert.NotNull(record);
        Assert.Equal(embedding, record.Embedding);
        Assert.Equal("hash1", record.ContentHash);
    }

    [Fact]
    public async Task SaveEmbedding_upserts_when_called_twice()
    {
        var path = "/vault/note.md";
        await _repo.SaveEmbeddingAsync(path, [1f, 2f, 3f], "hash1");
        await _repo.SaveEmbeddingAsync(path, [4f, 5f, 6f], "hash2");

        var all = await _repo.GetAllEmbeddingsAsync();
        Assert.Single(all);
        Assert.Equal([4f, 5f, 6f], all[0].Embedding);
        Assert.Equal("hash2", all[0].ContentHash);
    }

    [Fact]
    public async Task IsEmbeddingStale_returns_true_when_hash_differs()
    {
        await _repo.SaveEmbeddingAsync("/vault/note.md", [0.1f, 0.2f], "old-hash");
        Assert.True(await _repo.IsEmbeddingStaleAsync("/vault/note.md", "new-hash"));
    }

    [Fact]
    public async Task IsEmbeddingStale_returns_false_when_hash_matches()
    {
        await _repo.SaveEmbeddingAsync("/vault/note.md", [0.1f, 0.2f], "the-hash");
        Assert.False(await _repo.IsEmbeddingStaleAsync("/vault/note.md", "the-hash"));
    }

    [Fact]
    public async Task IsEmbeddingStale_returns_true_when_no_record()
    {
        Assert.True(await _repo.IsEmbeddingStaleAsync("/vault/never-saved.md", "any-hash"));
    }

    [Fact]
    public async Task UpdateEmbeddingPath_moves_record_to_new_key()
    {
        var oldPath = "/vault/old.md";
        var newPath = "/vault/sub/new.md";
        var embedding = new[] { 0.4f, 0.5f };
        await _repo.SaveEmbeddingAsync(oldPath, embedding, "h");

        await _repo.UpdateEmbeddingPathAsync(oldPath, newPath);

        Assert.Null(await _repo.GetEmbeddingAsync(oldPath));
        var moved = await _repo.GetEmbeddingAsync(newPath);
        Assert.NotNull(moved);
        Assert.Equal(embedding, moved.Embedding);
    }

    [Fact]
    public async Task UpdateEmbeddingPath_no_record_is_noop()
    {
        await _repo.UpdateEmbeddingPathAsync("/missing.md", "/also-missing.md");
        var all = await _repo.GetAllEmbeddingsAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task DeleteEmbedding_removes_record()
    {
        var path = "/vault/note.md";
        await _repo.SaveEmbeddingAsync(path, [0.1f], "h");

        await _repo.DeleteEmbeddingAsync(path);

        Assert.Null(await _repo.GetEmbeddingAsync(path));
    }

    [Fact]
    public async Task ClearAll_removes_embeddings_and_metadata()
    {
        await _repo.SaveEmbeddingAsync("/a.md", [0.1f], "h1");
        await _repo.SaveEmbeddingAsync("/b.md", [0.2f], "h2");
        await _repo.SetMetadataAsync("OpenAi", 1536, "text-embedding-3-small");

        await _repo.ClearAllAsync();

        Assert.Empty(await _repo.GetAllEmbeddingsAsync());
        Assert.Null(await _repo.GetMetadataAsync());
    }

    [Fact]
    public async Task Metadata_round_trips_provider_dimension_and_model()
    {
        await _repo.SetMetadataAsync("Gemini", 1536, "gemini-embedding-001");

        var metadata = await _repo.GetMetadataAsync();

        Assert.NotNull(metadata);
        Assert.Equal("Gemini", metadata.ProviderName);
        Assert.Equal(1536, metadata.Dimension);
        Assert.Equal("gemini-embedding-001", metadata.ModelId);
    }

    [Fact]
    public async Task Metadata_upserts_when_called_twice()
    {
        await _repo.SetMetadataAsync("OpenAi", 1536, "text-embedding-3-small");
        await _repo.SetMetadataAsync("OpenAi", 3072, "text-embedding-3-large");

        var metadata = await _repo.GetMetadataAsync();

        Assert.Equal(3072, metadata!.Dimension);
        Assert.Equal("text-embedding-3-large", metadata.ModelId);
    }

    [Fact]
    public async Task GetMetadata_when_unset_returns_null()
    {
        Assert.Null(await _repo.GetMetadataAsync());
    }

    [Fact]
    public async Task Schema_migration_adds_model_id_column_to_legacy_db()
    {
        var legacyPath = Path.Combine(Path.GetTempPath(), $"NoteForgeLegacy_{Guid.NewGuid():N}.db");
        try
        {
            using (var legacyConn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={legacyPath}"))
            {
                await legacyConn.OpenAsync();
                var cmd = legacyConn.CreateCommand();
                cmd.CommandText = """
                    CREATE TABLE metadata (
                        id INTEGER PRIMARY KEY CHECK (id = 1),
                        current_provider TEXT NOT NULL,
                        current_dimension INTEGER NOT NULL
                    );
                    INSERT INTO metadata (id, current_provider, current_dimension) VALUES (1, 'Ollama', 768);
                """;
                await cmd.ExecuteNonQueryAsync();
            }

            var migratedRepo = new EmbeddingRepository();
            await migratedRepo.InitializeAsync(legacyPath);
            try
            {
                var metadata = await migratedRepo.GetMetadataAsync();
                Assert.NotNull(metadata);
                Assert.Equal("Ollama", metadata.ProviderName);
                Assert.Equal(768, metadata.Dimension);
                Assert.Equal(string.Empty, metadata.ModelId);

                await migratedRepo.SetMetadataAsync("Ollama", 768, "nomic-embed-text");
                var updated = await migratedRepo.GetMetadataAsync();
                Assert.Equal("nomic-embed-text", updated!.ModelId);
            }
            finally
            {
                migratedRepo.Dispose();
            }
        }
        finally
        {
            if (File.Exists(legacyPath)) File.Delete(legacyPath);
        }
    }
}
