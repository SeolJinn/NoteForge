using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Models;

namespace NoteForge.Handlers;

public class MoveNoteToFolderCommandHandler : IRequestHandler<MoveNoteToFolderCommandRequest, bool>
{
    public async ValueTask<bool> Handle(MoveNoteToFolderCommandRequest request, CancellationToken cancellationToken)
    {
        var sourceFile = request.Note.FilePath;
        var targetFolder = request.TargetFolderPath;
        var fileName = Path.GetFileName(sourceFile);
        var targetFile = Path.Combine(targetFolder, fileName);

        if (File.Exists(targetFile))
        {
            return false;
        }

        try
        {
            await Task.Run(() => File.Move(sourceFile, targetFile), cancellationToken);
            request.Note.FilePath = targetFile;
            request.Note.Filename = Path.GetFileNameWithoutExtension(targetFile);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public sealed record MoveNoteToFolderCommandRequest(Note Note, string TargetFolderPath) : IRequest<bool>;