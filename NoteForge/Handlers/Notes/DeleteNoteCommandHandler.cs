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

public class DeleteNoteCommandHandler(
    IEmbeddingRepository embeddingRepository,
    ISemanticSearchStrategy semanticSearch,
    ILogger<DeleteNoteCommandHandler> logger) : IRequestHandler<DeleteNoteCommandRequest, OperationResult>
{
    public async ValueTask<OperationResult> Handle(DeleteNoteCommandRequest request, CancellationToken cancellationToken)
    {
        var note = request.Note;

        if (note is null || string.IsNullOrEmpty(note.FilePath))
        {
            return OperationResult.Fail("Invalid note.");
        }

        try
        {
            File.Delete(note.FilePath);

            try
            {
                if (embeddingRepository.IsInitialized)
                    await embeddingRepository.DeleteEmbeddingAsync(note.FilePath);

                semanticSearch.InvalidateIndex();
                semanticSearch.InvalidateEmbeddingsCache();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to clean up embedding for deleted note {Path}", note.FilePath);
            }

            return OperationResult.Ok();
        }
        catch (FileNotFoundException)
        {
            return OperationResult.Fail("Note file not found.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete note {Path}", note.FilePath);
            return OperationResult.Fail("Failed to delete the note. The file may be in use.");
        }
    }
}

public sealed class DeleteNoteCommandRequest(Note note) : IRequest<OperationResult>
{
    public Note Note { get; init; } = note;
}
