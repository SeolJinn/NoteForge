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

public class MoveNoteToFolderCommandHandler(
    INoteService noteService,
    IEmbeddingRepository embeddingRepository,
    IEmbeddingService embeddingService,
    ISemanticSearchStrategy semanticSearch,
    ILogger<MoveNoteToFolderCommandHandler> logger) : IRequestHandler<MoveNoteToFolderCommandRequest, OperationResult>
{
    public async ValueTask<OperationResult> Handle(MoveNoteToFolderCommandRequest request, CancellationToken cancellationToken)
    {
        var sourceFile = request.Note.FilePath;
        var targetFolder = request.TargetFolderPath;
        var fileName = Path.GetFileName(sourceFile);
        var targetFile = Path.Combine(targetFolder, fileName);

        if (!PathValidator.IsWithinVault(targetFile, noteService.CurrentNotebookPath))
        {
            return OperationResult.Fail("Target folder is outside the vault.");
        }

        if (File.Exists(targetFile))
        {
            return OperationResult.Fail("A note with that name already exists in the target folder.");
        }

        try
        {
            await Task.Run(() => File.Move(sourceFile, targetFile), cancellationToken);
            request.Note.FilePath = targetFile;
            request.Note.Filename = Path.GetFileNameWithoutExtension(targetFile);

            if (embeddingRepository.IsInitialized)
                await embeddingRepository.DeleteEmbeddingAsync(sourceFile);

            semanticSearch.InvalidateIndex();

            if (OllamaSettings.AiEnabled)
                embeddingService.QueueEmbeddingUpdate(request.Note, onComplete: semanticSearch.InvalidateEmbeddingsCache);

            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to move note from {Source} to {Target}", sourceFile, targetFile);
            return OperationResult.Fail("Failed to move the note. The file may be in use.");
        }
    }
}

public sealed record MoveNoteToFolderCommandRequest(Note Note, string TargetFolderPath) : IRequest<OperationResult>;
