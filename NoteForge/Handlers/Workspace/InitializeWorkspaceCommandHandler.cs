using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Configuration;
using NoteForge.Handlers.Notes;
using NoteForge.Interfaces;

namespace NoteForge.Handlers.Workspace;

public sealed class InitializeWorkspaceCommandHandler(
    INoteService noteService,
    ITabManager tabManager,
    IEmbeddingRepository embeddingRepository,
    IEmbeddingService embeddingService,
    IMediator mediator) : IRequestHandler<InitializeWorkspaceCommand, bool>
{
    public async ValueTask<bool> Handle(InitializeWorkspaceCommand request, CancellationToken cancellationToken)
    {
        if (!noteService.IsConfigured)
            return false;

        if (AiSettings.IsAiEnabled)
        {
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NoteForge",
                "Embeddings");

            Directory.CreateDirectory(appDataDir);

            var vaultPathHash = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(noteService.CurrentNotebookPath)))[..16];

            var dbPath = Path.Combine(appDataDir, $"{vaultPathHash}.db");
            embeddingService.CancelGeneration();
            await embeddingRepository.InitializeAsync(dbPath);
        }

        if (tabManager.Tabs.Count is 0)
        {
            tabManager.OpenNewTab();
        }

        if (AiSettings.IsAiEnabled)
        {
            var notes = (await mediator.Send(new GetNotesQueryRequest(), cancellationToken)).ToList();
            _ = embeddingService.EnsureEmbeddingsAsync(notes, cancellationToken);
        }

        return true;
    }
}

public sealed record InitializeWorkspaceCommand : IRequest<bool>;
