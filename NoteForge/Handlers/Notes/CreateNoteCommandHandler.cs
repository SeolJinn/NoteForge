using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using NoteForge.Configuration;
using NoteForge.Helpers;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers.Notes;

public class CreateNoteCommandHandler(
    INoteService noteService,
    IEmbeddingService embeddingService,
    ISemanticSearchStrategy semanticSearch,
    ILogger<CreateNoteCommandHandler> logger) : IRequestHandler<CreateNoteCommandRequest, Note?>
{
    public async ValueTask<Note?> Handle(CreateNoteCommandRequest request, CancellationToken cancellationToken)
    {
        if (!noteService.IsConfigured)
        {
            return null;
        }

        try
        {
            var targetPath = request.TargetFolderPath ?? noteService.CurrentNotebookPath;

            if (!PathValidator.IsWithinVault(targetPath, noteService.CurrentNotebookPath))
            {
                return null;
            }

            string baseName = "Untitled";
            string fileName = baseName + ".md";
            string filePath = Path.Combine(targetPath, fileName);

            int counter = 1;
            while (File.Exists(filePath))
            {
                fileName = $"{baseName} {counter}.md";
                filePath = Path.Combine(targetPath, fileName);
                counter++;
            }

            await File.WriteAllTextAsync(filePath, string.Empty, cancellationToken);

            var note = new Note
            {
                Filename = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath,
                Date = DateTime.Now,
                Text = string.Empty
            };

            semanticSearch.InvalidateIndex();

            if (AiSettings.IsAiEnabled)
                embeddingService.QueueEmbeddingUpdate(note, onComplete: semanticSearch.InvalidateEmbeddingsCache);

            return note;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create note");
            return null;
        }
    }
}

public sealed record CreateNoteCommandRequest(string? TargetFolderPath = null) : IRequest<Note?>;