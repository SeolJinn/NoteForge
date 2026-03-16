using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers.Folders;

public class CreateFolderCommandHandler(IFolderService folderService)
    : IRequestHandler<CreateFolderCommandRequest, OperationResult>
{
    public ValueTask<OperationResult> Handle(CreateFolderCommandRequest request, CancellationToken cancellationToken)
    {
        var result = folderService.CreateFolder(request.ParentPath, request.Name);
        return ValueTask.FromResult(result);
    }
}

public sealed record CreateFolderCommandRequest(string ParentPath, string Name) : IRequest<OperationResult>;
