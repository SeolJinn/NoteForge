using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Services.Embeddings;

public class EmbeddingService(
    IOllamaService ollamaService,
    IEmbeddingRepository embeddingRepo,
    ILogger<EmbeddingService> logger) : IEmbeddingService
{
    private CancellationTokenSource? _backgroundCts;
    private int _isGenerating;

    public event EventHandler<EmbeddingProgress>? ProgressChanged;
    public bool IsGenerating => Interlocked.CompareExchange(ref _isGenerating, 0, 0) is 1;

    public async Task StartBackgroundGenerationAsync(
        IEnumerable<Note> notes,
        CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _isGenerating, 1, 0) is 1)
        {
            logger.LogWarning("Background generation already in progress");
            return;
        }
        var previousCts = _backgroundCts;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _backgroundCts = cts;
        previousCts?.Dispose();

        await Task.Run(async () =>
        {
            List<Note> notesList = [.. notes];
            var progress = new EmbeddingProgress { TotalNotes = notesList.Count };

            try
            {
                foreach (var note in notesList)
                {
                    if (cts.Token.IsCancellationRequested)
                        break;

                    progress.CurrentNoteName = note.Filename;

                    try
                    {
                        if (string.IsNullOrWhiteSpace(note.Text) || note.Text.Length < 10)
                        {
                            progress.SkippedNotes++;
                            logger.LogDebug("Skipping note with insufficient content: {FileName}", note.Filename);
                            await embeddingRepo.DeleteEmbeddingAsync(note.FilePath);
                        }
                        else
                        {
                            var contentHash = ComputeContentHash(note);
                            var isStale = await embeddingRepo.IsEmbeddingStaleAsync(note.FilePath, contentHash);

                            if (!isStale)
                            {
                                progress.SkippedNotes++;
                                logger.LogDebug("Skipping up-to-date embedding for {FileName}", note.Filename);
                            }
                            else
                            {
                                await GenerateEmbeddingForNoteAsync(note, cts.Token);
                                logger.LogInformation("Generated embedding for {FileName}", note.Filename);
                            }
                        }

                        progress.ProcessedNotes++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to generate embedding for {FileName}", note.Filename);
                        progress.FailedNotes++;
                        progress.ProcessedNotes++;
                    }

                    ProgressChanged?.Invoke(this, progress);
                }

                progress.IsComplete = true;
                progress.CurrentNoteName = null;
                ProgressChanged?.Invoke(this, progress);

                logger.LogInformation(
                    "Background embedding generation complete: {Generated} generated, {Skipped} skipped, {Failed} failed",
                    progress.ProcessedNotes - progress.SkippedNotes - progress.FailedNotes,
                    progress.SkippedNotes,
                    progress.FailedNotes);
            }
            finally
            {
                Interlocked.Exchange(ref _isGenerating, 0);
            }
        }, cts.Token);
    }

    public async Task GenerateEmbeddingForNoteAsync(Note note, CancellationToken cancellationToken = default)
    {
        var text = BuildEmbeddingText(note);

        if (string.IsNullOrWhiteSpace(note.Text) || note.Text.Length < 10)
        {
            logger.LogDebug("Skipping embedding for note with insufficient content: {FileName}", note.Filename);
            await embeddingRepo.DeleteEmbeddingAsync(note.FilePath);
            return;
        }

        var preview = text.Length > 100 ? string.Concat(text.AsSpan(0, 100), "...") : text;
        logger.LogDebug("Generating embedding for {FileName} - Length: {Length} chars, Preview: {Preview}",
            note.Filename, text.Length, preview);

        var embedding = await ollamaService.GenerateEmbeddingAsync(text, cancellationToken);

        if (embedding is not null)
        {
            var magnitude = Math.Sqrt(embedding.Sum(v => v * v));
            var contentHash = ComputeContentHash(note);
            logger.LogDebug("Generated embedding - Magnitude: {Mag:F4}, Hash: {Hash}",
                magnitude, contentHash[..8]);
            await embeddingRepo.SaveEmbeddingAsync(note.FilePath, embedding, contentHash);
        }
        else
        {
            throw new InvalidOperationException("Failed to generate embedding from Ollama");
        }
    }

    public void QueueEmbeddingUpdate(Note note, string? oldPathToDelete = null, Action? onComplete = null)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                if (oldPathToDelete is not null)
                    await embeddingRepo.DeleteEmbeddingAsync(oldPathToDelete);

                await GenerateEmbeddingForNoteAsync(note);
                onComplete?.Invoke();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Background embedding update failed for {FilePath}", note.FilePath);
            }
        });
    }

    public void CancelGeneration()
    {
        if (_backgroundCts is not null && IsGenerating)
        {
            logger.LogInformation("Canceling background embedding generation");
            _backgroundCts.Cancel();
        }
    }

    private static string BuildEmbeddingText(Note note) => $"{note.Filename}\n\n{note.Text}";

    private static string ComputeContentHash(Note note)
    {
        var bytes = Encoding.UTF8.GetBytes(BuildEmbeddingText(note));
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
