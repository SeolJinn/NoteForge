using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using NoteForge.Helpers;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers.Notes;

public class RenameNoteCommandHandler(
    INoteService noteService,
    IEmbeddingService embeddingService,
    ISemanticSearchStrategy semanticSearch,
    ILogger<RenameNoteCommandHandler> logger) : IRequestHandler<RenameNoteCommandRequest, OperationResult>
{
    public async ValueTask<OperationResult> Handle(RenameNoteCommandRequest request, CancellationToken cancellationToken)
    {
        var note = request.Note;
        var newName = request.NewName;

        if (string.IsNullOrWhiteSpace(newName) || note is null || string.IsNullOrEmpty(note.FilePath))
        {
            return OperationResult.Fail("Invalid note name.");
        }

        if (!newName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            newName += ".md";
        }

        var directory = Path.GetDirectoryName(note.FilePath);
        if (string.IsNullOrEmpty(directory))
        {
            return OperationResult.Fail("Could not determine note directory.");
        }

        var newPath = Path.Combine(directory, newName);

        if (!PathValidator.IsWithinVault(newPath, noteService.CurrentNotebookPath))
        {
            return OperationResult.Fail("The name contains invalid path characters.");
        }

        if (string.Equals(note.FilePath, newPath, StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Ok();
        }

        if (File.Exists(newPath))
        {
            return OperationResult.Fail("A note with that name already exists.");
        }

        try
        {
            var oldPath = note.FilePath;
            await Task.Run(() => File.Move(oldPath, newPath), cancellationToken);

            note.FilePath = newPath;
            note.Filename = Path.GetFileNameWithoutExtension(newPath);

            semanticSearch.InvalidateIndex();
            embeddingService.QueueEmbeddingUpdate(note, oldPathToDelete: oldPath, onComplete: semanticSearch.InvalidateEmbeddingsCache);

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rename note from {OldPath} to {NewPath}", note.FilePath, newPath);
            return OperationResult.Fail("Failed to rename the note. The file may be in use.");
        }
    }
}

public sealed class RenameNoteCommandRequest(Note note, string newName) : IRequest<OperationResult>
{
    public Note Note { get; init; } = note;
    public string NewName { get; init; } = newName;
}
