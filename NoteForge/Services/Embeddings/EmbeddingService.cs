using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NoteForge.Models;

namespace NoteForge.Services.Embeddings;

public class EmbeddingService
{
    private readonly OllamaService _ollamaService;
    private readonly EmbeddingRepository _embeddingRepo;
    private readonly ILogger<EmbeddingService> _logger;
    private CancellationTokenSource? _backgroundCts;

    public event EventHandler<EmbeddingProgress>? ProgressChanged;
    public bool IsGenerating { get; private set; }

    public EmbeddingService(
        OllamaService ollamaService,
        EmbeddingRepository embeddingRepo,
        ILogger<EmbeddingService> logger)
    {
        _ollamaService = ollamaService;
        _embeddingRepo = embeddingRepo;
        _logger = logger;
    }

    public async Task StartBackgroundGenerationAsync(
        IEnumerable<Note> notes,
        CancellationToken cancellationToken = default)
    {
        if (IsGenerating)
        {
            _logger.LogWarning("Background generation already in progress");
            return;
        }

        IsGenerating = true;
        _backgroundCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await Task.Run(async () =>
        {
            var notesList = notes.ToList();
            var progress = new EmbeddingProgress { TotalNotes = notesList.Count };

            try
            {
                foreach (var note in notesList)
                {
                    if (_backgroundCts.Token.IsCancellationRequested)
                        break;

                    progress.CurrentNoteName = note.Filename;

                    try
                    {
                        if (string.IsNullOrWhiteSpace(note.Text) || note.Text.Length < 10)
                        {
                            progress.SkippedNotes++;
                            _logger.LogDebug("Skipping note with insufficient content: {FileName}", note.Filename);
                            await _embeddingRepo.DeleteEmbeddingAsync(note.FilePath);
                        }
                        else
                        {
                            var contentHash = ComputeContentHash(note);
                            var isStale = await _embeddingRepo.IsEmbeddingStaleAsync(note.FilePath, contentHash);

                            if (!isStale)
                            {
                                progress.SkippedNotes++;
                                _logger.LogDebug("Skipping up-to-date embedding for {FileName}", note.Filename);
                            }
                            else
                            {
                                await GenerateEmbeddingForNoteAsync(note, _backgroundCts.Token);
                                _logger.LogInformation("Generated embedding for {FileName}", note.Filename);
                            }
                        }

                        progress.ProcessedNotes++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate embedding for {FileName}", note.Filename);
                        progress.FailedNotes++;
                        progress.ProcessedNotes++;
                    }

                    ProgressChanged?.Invoke(this, progress);
                }

                progress.IsComplete = true;
                progress.CurrentNoteName = null;
                ProgressChanged?.Invoke(this, progress);

                _logger.LogInformation(
                    "Background embedding generation complete: {Generated} generated, {Skipped} skipped, {Failed} failed",
                    progress.ProcessedNotes - progress.SkippedNotes - progress.FailedNotes,
                    progress.SkippedNotes,
                    progress.FailedNotes);
            }
            finally
            {
                IsGenerating = false;
            }
        }, _backgroundCts.Token);
    }

    public async Task GenerateEmbeddingForNoteAsync(Note note, CancellationToken cancellationToken = default)
    {
        var text = $"{note.Filename}\n\n{note.Text}";

        if (string.IsNullOrWhiteSpace(note.Text) || note.Text.Length < 10)
        {
            _logger.LogDebug("Skipping embedding for note with insufficient content: {FileName}", note.Filename);
            await _embeddingRepo.DeleteEmbeddingAsync(note.FilePath);
            return;
        }

        var preview = text.Length > 100 ? text.Substring(0, 100) + "..." : text;
        _logger.LogDebug("Generating embedding for {FileName} - Length: {Length} chars, Preview: {Preview}",
            note.Filename, text.Length, preview);

        var embedding = await _ollamaService.GenerateEmbeddingAsync(text, cancellationToken);

        if (embedding is not null)
        {
            var magnitude = Math.Sqrt(embedding.Sum(v => v * v));
            var contentHash = ComputeContentHash(note);
            _logger.LogDebug("Generated embedding - Magnitude: {Mag:F4}, Hash: {Hash}",
                magnitude, contentHash.Substring(0, 8));
            await _embeddingRepo.SaveEmbeddingAsync(note.FilePath, embedding, contentHash);
        }
        else
        {
            throw new InvalidOperationException("Failed to generate embedding from Ollama");
        }
    }

    public void CancelGeneration()
    {
        if (_backgroundCts is not null && IsGenerating)
        {
            _logger.LogInformation("Canceling background embedding generation");
            _backgroundCts.Cancel();
        }
    }

    private static string ComputeContentHash(Note note)
    {
        var text = $"{note.Filename}\n\n{note.Text}";
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
