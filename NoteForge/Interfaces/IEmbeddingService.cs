using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NoteForge.Models;

namespace NoteForge.Interfaces;

public interface IEmbeddingService
{
    event EventHandler<EmbeddingProgress>? ProgressChanged;
    bool IsGenerating { get; }
    Task StartBackgroundGenerationAsync(IEnumerable<Note> notes, CancellationToken cancellationToken = default);
    Task GenerateEmbeddingForNoteAsync(Note note, CancellationToken cancellationToken = default);
    void QueueEmbeddingUpdate(Note note, string? oldPathToDelete = null, Action? onComplete = null);
    void CancelGeneration();
    Task RegenerateAllAsync(IEnumerable<Note> notes, CancellationToken cancellationToken = default);
}
