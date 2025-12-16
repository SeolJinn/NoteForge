using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Services;

namespace NoteForge.Handlers.Folders;

public class RenameFolderCommandHandler(FolderService folderService)
    : IRequestHandler<RenameFolderCommandRequest, bool>
{
    public ValueTask<bool> Handle(RenameFolderCommandRequest request, CancellationToken cancellationToken)
    {
        var result = folderService.RenameFolder(request.FolderPath, request.NewName);
        return ValueTask.FromResult(result);
    }
}

public sealed record RenameFolderCommandRequest(string FolderPath, string NewName) : IRequest<bool>;