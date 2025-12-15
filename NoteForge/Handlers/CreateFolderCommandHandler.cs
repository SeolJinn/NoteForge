using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Services;

namespace NoteForge.Handlers;

public class CreateFolderCommandHandler(FolderService folderService)
    : IRequestHandler<CreateFolderCommandRequest, bool>
{
    public ValueTask<bool> Handle(CreateFolderCommandRequest request, CancellationToken cancellationToken)
    {
        var result = folderService.CreateFolder(request.ParentPath, request.Name);
        return ValueTask.FromResult(result);
    }
}

public sealed record CreateFolderCommandRequest(string ParentPath, string Name) : IRequest<bool>;