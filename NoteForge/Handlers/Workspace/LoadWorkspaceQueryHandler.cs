using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Handlers.Notes;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers.Workspace;

public sealed class LoadWorkspaceQueryHandler(
    INoteService noteService,
    ITabManager tabManager,
    ISectionService sectionService,
    IFolderService folderService,
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
                RootFolder: null,
                FavoritesSection: null,
                InitialNoteFilePath: null);
        }

        var notes = (await mediator.Send(new GetNotesQueryRequest(), cancellationToken)).ToList();

        var rootFolder = folderService.BuildFolderTree(noteService.CurrentNotebookPath);
        folderService.LoadExpandedState(rootFolder);

        var favoritesSection = sectionService.GetFavoritesSection(notes);

        var initialNoteFilePath = tabManager.ActiveTab is { IsNewTab: false }
            ? tabManager.ActiveTab.FilePath
            : null;

        return new LoadWorkspaceQueryResponse(
            IsConfigured: true,
            VaultName: noteService.CurrentVaultName,
            VaultPath: $"Path: {noteService.CurrentNotebookPath}",
            RootFolder: rootFolder,
            FavoritesSection: favoritesSection,
            InitialNoteFilePath: initialNoteFilePath);
    }
}

public sealed record LoadWorkspaceQueryRequest : IRequest<LoadWorkspaceQueryResponse>;

public sealed record LoadWorkspaceQueryResponse(
    bool IsConfigured,
    string VaultName,
    string VaultPath,
    Folder? RootFolder,
    NoteSection? FavoritesSection,
    string? InitialNoteFilePath);
