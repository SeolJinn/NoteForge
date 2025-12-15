using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Services;

namespace NoteForge.Handlers;

public class DeleteFolderCommandHandler(FolderService folderService)
    : IRequestHandler<DeleteFolderCommandRequest, bool>
{
    public ValueTask<bool> Handle(DeleteFolderCommandRequest request, CancellationToken cancellationToken)
    {
        if (!folderService.IsFolderEmpty(request.FolderPath))
        {
            return ValueTask.FromResult(false);
        }

        var result = folderService.DeleteFolder(request.FolderPath);
        return ValueTask.FromResult(result);
    }
}

public sealed record DeleteFolderCommandRequest(string FolderPath) : IRequest<bool>;