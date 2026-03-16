using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers.Folders;

public class RenameFolderCommandHandler(IFolderService folderService)
    : IRequestHandler<RenameFolderCommandRequest, OperationResult>
{
    public ValueTask<OperationResult> Handle(RenameFolderCommandRequest request, CancellationToken cancellationToken)
    {
        var result = folderService.RenameFolder(request.FolderPath, request.NewName);
        return ValueTask.FromResult(result);
    }
}

public sealed record RenameFolderCommandRequest(string FolderPath, string NewName) : IRequest<OperationResult>;
