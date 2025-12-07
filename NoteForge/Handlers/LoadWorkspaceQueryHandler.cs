using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers;

public sealed class LoadWorkspaceQueryHandler(
    INoteService noteService,
    ITabManager tabManager,
    IMediator mediator) : IRequestHandler<LoadWorkspaceQueryRequest, LoadWorkspaceQueryResponse>
{
    public async ValueTask<LoadWorkspaceQueryResponse> Handle(LoadWorkspaceQueryRequest request, CancellationToken cancellationToken)
    {
        if (!noteService.IsConfigured)
        {
            return new LoadWorkspaceQueryResponse(
                IsConfigured: false,
                VaultName: "No vault selected",
                VaultPath: "No vault selected",
                Notes: [],
                InitialNoteFilePath: null);
        }

        var notes = (await mediator.Send(new GetNotesQueryRequest(), cancellationToken)).ToList();

        if (tabManager.Tabs.Count == 0)
        {
            tabManager.OpenNewTab();
        }

        var initialNoteFilePath = tabManager.ActiveTab is { IsNewTab: false }
            ? tabManager.ActiveTab.FilePath
            : null;

        return new LoadWorkspaceQueryResponse(
            IsConfigured: true,
            VaultName: noteService.CurrentVaultName,
            VaultPath: $"Path: {noteService.CurrentNotebookPath}",
            Notes: notes,
            InitialNoteFilePath: initialNoteFilePath);
    }
}

public sealed record LoadWorkspaceQueryRequest : IRequest<LoadWorkspaceQueryResponse>;

public sealed record LoadWorkspaceQueryResponse(
    bool IsConfigured,
    string VaultName,
    string VaultPath,
    List<Note> Notes,
    string? InitialNoteFilePath);