using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NoteForge.Interfaces;

namespace NoteForge.Services.Ai;

public sealed class DisabledAiProvider : IAiService
{
    public int EmbeddingDimension => 0;

    public async IAsyncEnumerable<string> StreamCompletionAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        throw new AiDisabledException();
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    public Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        => throw new AiDisabledException();

    public Task<TestConnectionResult> TestConnectionAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new TestConnectionResult(false, "AI is disabled."));

    public void Dispose() { }
}
