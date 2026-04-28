using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Services.Embeddings;

public class EmbeddingRepository : IEmbeddingRepository
{
    private SqliteConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsInitialized => _connection is not null;

    public async Task InitializeAsync(string dbPath)
    {
        await _lock.WaitAsync();
        try
        {
            _connection?.Dispose();
            _connection = new SqliteConnection($"Data Source={dbPath}");
            await _connection.OpenAsync();

            var createTableCommand = _connection.CreateCommand();
            createTableCommand.CommandText = """
                CREATE TABLE IF NOT EXISTS embeddings (
                    file_path TEXT PRIMARY KEY,
                    embedding BLOB NOT NULL,
                    content_hash TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    dimension INTEGER NOT NULL DEFAULT 768
                );
                CREATE INDEX IF NOT EXISTS idx_updated_at ON embeddings(updated_at);
                CREATE TABLE IF NOT EXISTS metadata (
                    id INTEGER PRIMARY KEY CHECK (id = 1),
                    current_provider TEXT NOT NULL,
                    current_dimension INTEGER NOT NULL,
                    current_model_id TEXT NOT NULL DEFAULT ''
                );
            """;
            await createTableCommand.ExecuteNonQueryAsync();

            var migrateMetadataCommand = _connection.CreateCommand();
            migrateMetadataCommand.CommandText = """
                SELECT COUNT(*) FROM pragma_table_info('metadata') WHERE name = 'current_model_id';
            """;
            var hasModelColumn = Convert.ToInt32(await migrateMetadataCommand.ExecuteScalarAsync()) > 0;
            if (!hasModelColumn)
            {
                var alterCommand = _connection.CreateCommand();
                alterCommand.CommandText = "ALTER TABLE metadata ADD COLUMN current_model_id TEXT NOT NULL DEFAULT '';";
                await alterCommand.ExecuteNonQueryAsync();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> SaveEmbeddingAsync(string filePath, float[] embedding, string contentHash)
    {
        await _lock.WaitAsync();
        try
        {
            if (_connection is null)
                return false;

            var embeddingBytes = new byte[embedding.Length * sizeof(float)];
            Buffer.BlockCopy(embedding, 0, embeddingBytes, 0, embeddingBytes.Length);

            var now = DateTime.UtcNow.ToString("o");

            var command = _connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO embeddings (file_path, embedding, content_hash, created_at, updated_at, dimension)
                VALUES ($filePath, $embedding, $contentHash, $createdAt, $updatedAt, $dimension)
                ON CONFLICT(file_path) DO UPDATE SET
                    embedding = $embedding,
                    content_hash = $contentHash,
                    updated_at = $updatedAt,
                    dimension = $dimension
            ";
            command.Parameters.AddWithValue("$filePath", filePath);
            command.Parameters.AddWithValue("$embedding", embeddingBytes);
            command.Parameters.AddWithValue("$contentHash", contentHash);
            command.Parameters.AddWithValue("$createdAt", now);
            command.Parameters.AddWithValue("$updatedAt", now);
            command.Parameters.AddWithValue("$dimension", embedding.Length);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<EmbeddingRecord?> GetEmbeddingAsync(string filePath)
    {
        await _lock.WaitAsync();
        try
        {
            if (_connection is null)
                return null;

            var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT file_path, embedding, content_hash, created_at, updated_at, dimension
                FROM embeddings
                WHERE file_path = $filePath
            ";
            command.Parameters.AddWithValue("$filePath", filePath);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadEmbeddingRecord(reader);
            }

            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<EmbeddingRecord>> GetAllEmbeddingsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_connection is null)
                return [];

            List<EmbeddingRecord> results = [];
            var command = _connection.CreateCommand();
            command.CommandText = @"
                SELECT file_path, embedding, content_hash, created_at, updated_at, dimension
                FROM embeddings
            ";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(ReadEmbeddingRecord(reader));
            }

            return results;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteEmbeddingAsync(string filePath)
    {
        await _lock.WaitAsync();
        try
        {
            if (_connection is null)
                return;

            var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM embeddings WHERE file_path = $filePath";
            command.Parameters.AddWithValue("$filePath", filePath);

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpdateEmbeddingPathAsync(string oldPath, string newPath)
    {
        await _lock.WaitAsync();
        try
        {
            if (_connection is null) return;

            var command = _connection.CreateCommand();
            command.CommandText = "UPDATE embeddings SET file_path = $newPath WHERE file_path = $oldPath";
            command.Parameters.AddWithValue("$oldPath", oldPath);
            command.Parameters.AddWithValue("$newPath", newPath);
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> IsEmbeddingStaleAsync(string filePath, string currentContentHash)
    {
        await _lock.WaitAsync();
        try
        {
            if (_connection is null)
                return true;

            var command = _connection.CreateCommand();
            command.CommandText = "SELECT content_hash FROM embeddings WHERE file_path = $filePath";
            command.Parameters.AddWithValue("$filePath", filePath);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var storedHash = reader.GetString(0);
                return storedHash != currentContentHash;
            }

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static EmbeddingRecord ReadEmbeddingRecord(SqliteDataReader reader)
    {
        var embeddingBytes = (byte[])reader["embedding"];
        var dimension = reader.GetInt32(5);
        var embedding = new float[dimension];
        Buffer.BlockCopy(embeddingBytes, 0, embedding, 0, embeddingBytes.Length);

        return new EmbeddingRecord
        {
            FilePath = reader.GetString(0),
            Embedding = embedding,
            ContentHash = reader.GetString(2),
            CreatedAt = DateTime.Parse(reader.GetString(3)),
            UpdatedAt = DateTime.Parse(reader.GetString(4))
        };
    }

    public async Task<EmbeddingMetadata?> GetMetadataAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_connection is null) return null;

            var command = _connection.CreateCommand();
            command.CommandText = "SELECT current_provider, current_dimension, current_model_id FROM metadata WHERE id = 1";

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new EmbeddingMetadata(reader.GetString(0), reader.GetInt32(1), reader.GetString(2));
            }
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SetMetadataAsync(string providerName, int dimension, string modelId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_connection is null) return;

            var command = _connection.CreateCommand();
            command.CommandText = """
                INSERT INTO metadata (id, current_provider, current_dimension, current_model_id)
                VALUES (1, $provider, $dimension, $modelId)
                ON CONFLICT(id) DO UPDATE SET
                    current_provider = $provider,
                    current_dimension = $dimension,
                    current_model_id = $modelId
            """;
            command.Parameters.AddWithValue("$provider", providerName);
            command.Parameters.AddWithValue("$dimension", dimension);
            command.Parameters.AddWithValue("$modelId", modelId);
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_connection is null) return;

            var command = _connection.CreateCommand();
            command.CommandText = "DELETE FROM embeddings; DELETE FROM metadata;";
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _lock.Wait();
        try
        {
            _connection?.Dispose();
            _connection = null;
        }
        finally
        {
            _lock.Release();
        }
    }
}
