using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using NoteForge.Handlers.Notes;
using NoteForge.Interfaces;
using NoteForge.Models;
using NoteForge.Services;
using NoteForge.Services.Embeddings;

namespace NoteForge.Handlers.Workspace;

public sealed class LoadWorkspaceQueryHandler(
    INoteService noteService,
    ITabManager tabManager,
    SectionService sectionService,
    FolderService folderService,
    OllamaService ollamaService,
    IMediator mediator,
    ILogger<LoadWorkspaceQueryHandler> logger) : IRequestHandler<LoadWorkspaceQueryRequest, LoadWorkspaceQueryResponse>
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

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NoteForge",
            "Embeddings");

        if (!Directory.Exists(appDataDir))
        {
            Directory.CreateDirectory(appDataDir);
        }

        var vaultPathHash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(noteService.CurrentNotebookPath)))[..16];

        var dbPath = Path.Combine(appDataDir, $"{vaultPathHash}.db");
        App.EmbeddingRepository?.Dispose();
        App.EmbeddingRepository = new EmbeddingRepository(dbPath);
        await App.EmbeddingRepository.InitializeDatabaseAsync();

        App.EmbeddingService = new EmbeddingService(
            ollamaService,
            App.EmbeddingRepository,
            App.LoggerFactory.CreateLogger<EmbeddingService>());

        var notes = (await mediator.Send(new GetNotesQueryRequest(), cancellationToken)).ToList();

        var rootFolder = folderService.BuildFolderTree(noteService.CurrentNotebookPath);
        folderService.LoadExpandedState(rootFolder);

        var favoritesSection = sectionService.GetFavoritesSection(notes);

        if (tabManager.Tabs.Count is 0)
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