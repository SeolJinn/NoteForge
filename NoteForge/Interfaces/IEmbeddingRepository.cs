using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NoteForge.Models;

namespace NoteForge.Interfaces;

public interface IEmbeddingRepository : IDisposable
{
    bool IsInitialized { get; }
    Task InitializeAsync(string dbPath);
    Task<bool> SaveEmbeddingAsync(string filePath, float[] embedding, string contentHash);
    Task<EmbeddingRecord?> GetEmbeddingAsync(string filePath);
    Task<List<EmbeddingRecord>> GetAllEmbeddingsAsync();
    Task DeleteEmbeddingAsync(string filePath);
    Task UpdateEmbeddingPathAsync(string oldPath, string newPath);
    Task<bool> IsEmbeddingStaleAsync(string filePath, string currentContentHash);
    Task<EmbeddingMetadata?> GetMetadataAsync();
    Task SetMetadataAsync(string providerName, int dimension, string modelId);
    Task ClearAllAsync();
}

public sealed record EmbeddingMetadata(string ProviderName, int Dimension, string ModelId);
