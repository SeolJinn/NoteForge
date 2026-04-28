using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NoteForge.Interfaces;

public interface IAiService : IDisposable
{
    int EmbeddingDimension { get; }
    IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken cancellationToken = default);
    Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<TestConnectionResult> TestConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed record TestConnectionResult(bool Success, string? ErrorMessage = null);
