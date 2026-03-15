using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NoteForge.Models;

namespace NoteForge.Services.Embeddings;

public class EmbeddingRepository : IDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _connection;

    public EmbeddingRepository(string dbPath)
    {
        _dbPath = dbPath;
    }

    public async Task InitializeDatabaseAsync()
    {
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        await _connection.OpenAsync();

        var createTableCommand = _connection.CreateCommand();
        createTableCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS embeddings (
                file_path TEXT PRIMARY KEY,
                embedding BLOB NOT NULL,
                content_hash TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                dimension INTEGER NOT NULL DEFAULT 768
            );
            CREATE INDEX IF NOT EXISTS idx_updated_at ON embeddings(updated_at);
        ";
        await createTableCommand.ExecuteNonQueryAsync();
    }

    public async Task<bool> SaveEmbeddingAsync(string filePath, float[] embedding, string contentHash)
    {
        if (_connection is null)
            throw new InvalidOperationException("Database not initialized");

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

    public async Task<EmbeddingRecord?> GetEmbeddingAsync(string filePath)
    {
        if (_connection is null)
            throw new InvalidOperationException("Database not initialized");

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

    public async Task<List<EmbeddingRecord>> GetAllEmbeddingsAsync()
    {
        if (_connection is null)
            throw new InvalidOperationException("Database not initialized");

        var results = new List<EmbeddingRecord>();
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

    public async Task DeleteEmbeddingAsync(string filePath)
    {
        if (_connection is null)
            throw new InvalidOperationException("Database not initialized");

        var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM embeddings WHERE file_path = $filePath";
        command.Parameters.AddWithValue("$filePath", filePath);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> IsEmbeddingStaleAsync(string filePath, string currentContentHash)
    {
        if (_connection is null)
            throw new InvalidOperationException("Database not initialized");

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

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
