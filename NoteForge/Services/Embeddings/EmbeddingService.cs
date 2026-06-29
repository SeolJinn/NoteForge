using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    IAiService aiService,
    IEmbeddingRepository embeddingRepo,
    ILogger<EmbeddingService> logger) : IEmbeddingService
{
    internal static TimeSpan UpdateDebounce { get; set; } = TimeSpan.FromSeconds(5);

    private const int TimingCheckpointInterval = 25;

    private CancellationTokenSource? _backgroundCts;
    private int _isGenerating;
    private readonly ConcurrentDictionary<string, object> _pendingUpdates = new();

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

            var stopwatch = Stopwatch.StartNew();
            List<(int generated, double seconds)> timingPoints = [];
            var generated = 0;

            try
            {
                try
                {
                    await embeddingRepo.SetMetadataAsync(
                        NoteForge.Configuration.AiSettings.ActiveProvider.ToString(),
                        aiService.EmbeddingDimension,
                        GetActiveEmbeddingModelId());
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to set embedding metadata");
                }

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

                                generated++;
                                if (generated % TimingCheckpointInterval is 0)
                                {
                                    var elapsed = stopwatch.Elapsed.TotalSeconds;
                                    timingPoints.Add((generated, elapsed));
                                    logger.LogInformation("Embedding timing: {Generated} notes in {Seconds:F1}s", generated, elapsed);
                                }
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

                if (generated > 0)
                {
                    if (timingPoints.Count is 0 || timingPoints[^1].generated != generated)
                        timingPoints.Add((generated, stopwatch.Elapsed.TotalSeconds));
                    WriteTimingLog(timingPoints);
                }
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

        var embedding = await aiService.GenerateEmbeddingAsync(text, cancellationToken);

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
            throw new InvalidOperationException("Failed to generate embedding.");
        }
    }

    public void QueueEmbeddingUpdate(Note note, string? oldPathToDelete = null, Action? onComplete = null)
    {
        if (oldPathToDelete is not null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await embeddingRepo.DeleteEmbeddingAsync(oldPathToDelete);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete old embedding {FilePath}", oldPathToDelete);
                }
            });
        }

        var token = new object();
        var key = note.FilePath;
        _pendingUpdates[key] = token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(UpdateDebounce);

                if (!_pendingUpdates.TryGetValue(key, out var current) || current != token)
                {
                    return;
                }
                _pendingUpdates.TryRemove(new KeyValuePair<string, object>(key, token));

                if (!string.IsNullOrWhiteSpace(note.Text) && note.Text.Length >= 10)
                {
                    var contentHash = ComputeContentHash(note);
                    if (!await embeddingRepo.IsEmbeddingStaleAsync(note.FilePath, contentHash))
                    {
                        logger.LogDebug("Skipping embedding update — content unchanged: {FilePath}", note.FilePath);
                        onComplete?.Invoke();
                        return;
                    }
                }

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

    public async Task RegenerateAllAsync(IEnumerable<Note> notes, CancellationToken cancellationToken = default)
    {
        CancelGeneration();
        await embeddingRepo.ClearAllAsync();
        await embeddingRepo.SetMetadataAsync(
            NoteForge.Configuration.AiSettings.ActiveProvider.ToString(),
            aiService.EmbeddingDimension,
            GetActiveEmbeddingModelId());
        await StartBackgroundGenerationAsync(notes, cancellationToken);
    }

    public async Task EnsureEmbeddingsAsync(IEnumerable<Note> notes, CancellationToken cancellationToken = default)
    {
        if (await StoredEmbeddingsMatchActiveProviderAsync())
        {
            await StartBackgroundGenerationAsync(notes, cancellationToken);
        }
        else
        {
            logger.LogInformation("Stored embeddings do not match the active provider; regenerating");
            await RegenerateAllAsync(notes, cancellationToken);
        }
    }

    private async Task<bool> StoredEmbeddingsMatchActiveProviderAsync()
    {
        if (!embeddingRepo.IsInitialized)
            return true;

        if (await embeddingRepo.CountEmbeddingsAsync() is 0)
            return true;

        var metadata = await embeddingRepo.GetMetadataAsync();
        if (metadata is null)
            return false;

        if (metadata.ProviderName != NoteForge.Configuration.AiSettings.ActiveProvider.ToString()
            || metadata.Dimension != aiService.EmbeddingDimension)
            return false;

        return string.IsNullOrEmpty(metadata.ModelId) || metadata.ModelId == GetActiveEmbeddingModelId();
    }

    private static string GetActiveEmbeddingModelId() =>
        NoteForge.Configuration.AiSettings.ActiveProvider switch
        {
            NoteForge.Services.Ai.AiProviderType.OpenAi => NoteForge.Configuration.AiSettings.OpenAiEmbeddingModel,
            NoteForge.Services.Ai.AiProviderType.Gemini => NoteForge.Configuration.AiSettings.GeminiEmbeddingModel,
            NoteForge.Services.Ai.AiProviderType.Ollama => NoteForge.Configuration.AiSettings.OllamaEmbeddingModel,
            _ => string.Empty
        };

    private void WriteTimingLog(List<(int generated, double seconds)> points)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NoteForge");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "embedding-timing.txt");

            var sb = new StringBuilder();
            sb.AppendLine("=== Embedding timing run (cold cache) ===");
            sb.AppendLine($"Provider: {NoteForge.Configuration.AiSettings.ActiveProvider}  Model: {GetActiveEmbeddingModelId()}  Dimension: {aiService.EmbeddingDimension}");
            sb.AppendLine("notes\tseconds\tms/note");
            foreach (var (generated, seconds) in points)
                sb.AppendLine($"{generated}\t{seconds:F1}\t{seconds / generated * 1000:F0}");
            sb.AppendLine();

            File.AppendAllText(path, sb.ToString());
            logger.LogInformation("Embedding timing written to {Path}", path);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write embedding timing log");
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
