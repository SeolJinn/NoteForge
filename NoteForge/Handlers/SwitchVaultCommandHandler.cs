using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Interfaces;

namespace NoteForge.Handlers;

public sealed class SwitchVaultCommandHandler(INoteService noteService) : IRequestHandler<SwitchVaultCommandRequest, SwitchVaultCommandResponse>
{
    public ValueTask<SwitchVaultCommandResponse> Handle(SwitchVaultCommandRequest request, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(request.VaultPath))
        {
            return ValueTask.FromResult(new SwitchVaultCommandResponse(false, "Vault folder no longer exists."));
        }

        noteService.SetVaultPath(request.VaultPath);
        return ValueTask.FromResult(new SwitchVaultCommandResponse(true, null));
    }
}

public sealed record SwitchVaultCommandRequest(string VaultPath) : IRequest<SwitchVaultCommandResponse>;

public sealed record SwitchVaultCommandResponse(bool Success, string? ErrorMessage);