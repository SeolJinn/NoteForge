using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers.Folders;

public class DeleteFolderCommandHandler(IFolderService folderService)
    : IRequestHandler<DeleteFolderCommandRequest, OperationResult>
{
    public ValueTask<OperationResult> Handle(DeleteFolderCommandRequest request, CancellationToken cancellationToken)
    {
        var result = folderService.DeleteFolder(request.FolderPath);
        return ValueTask.FromResult(result);
    }
}

public sealed record DeleteFolderCommandRequest(string FolderPath) : IRequest<OperationResult>;
