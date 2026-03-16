using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NoteForge.Interfaces;

public interface IOllamaService : IDisposable
{
    IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CancellationToken cancellationToken = default);
    Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
