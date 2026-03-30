using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using NoteForge.Configuration;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers.Notes;

public class SaveNoteCommandHandler(
    IEmbeddingService embeddingService,
    ISemanticSearchStrategy semanticSearch,
    ILogger<SaveNoteCommandHandler> logger) : IRequestHandler<SaveNoteCommandRequest, bool>
{
    public async ValueTask<bool> Handle(SaveNoteCommandRequest request, CancellationToken cancellationToken)
    {
        if (request.Note is null || string.IsNullOrEmpty(request.Note.FilePath))
        {
            return false;
        }

        try
        {
            var targetPath = request.Note.FilePath;
            var tempPath = targetPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, request.Note.Text, cancellationToken);
            File.Move(tempPath, targetPath, overwrite: true);
            semanticSearch.InvalidateIndex();

            if (OllamaSettings.AiEnabled)
                embeddingService.QueueEmbeddingUpdate(request.Note, onComplete: semanticSearch.InvalidateEmbeddingsCache);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save note: {FilePath}", request.Note.FilePath);
            return false;
        }
    }
}

public sealed class SaveNoteCommandRequest(Note note) : IRequest<bool>
{
    public Note Note { get; init; } = note;
}