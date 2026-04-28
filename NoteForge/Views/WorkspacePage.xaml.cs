using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Controls;
using NoteForge.Handlers.Notes;
using NoteForge.Handlers.Workspace;
using NoteForge.Interfaces;
using NoteForge.Models;
using NoteForge.Services;

namespace NoteForge.Views;

public sealed partial class WorkspacePage : Page
{
    private readonly INoteService _noteService;
    private readonly ITabManager _tabManager;
    private readonly IDialogService _dialogService;
    private readonly IMediator _mediator;
    private readonly ILogger<WorkspacePage> _logger;
    private readonly IFolderDialogService _folderDialogService;
    private readonly SidebarCoordinator _sidebarCoordinator;
    private Note? _selectedNote;
    private CancellationTokenSource? _summaryCts;
    private bool _isLoading;
    private bool _isSyncingTitle;
    private QuickFileNavigator? _quickFileNavigator;
    private string? _pendingTitleRename;

    public WorkspacePage()
    {
        InitializeComponent();
        _noteService = App.NoteService;
        _tabManager = App.TabManager;
        _dialogService = App.DialogService;
        _mediator = App.Mediator;
        _logger = App.LoggerFactory.CreateLogger<WorkspacePage>();
        _folderDialogService = App.Services.GetRequiredService<IFolderDialogService>();
        _sidebarCoordinator = App.Services.GetRequiredService<SidebarCoordinator>();

        TabBarControl.SetItemsSource(_tabManager.Tabs);

        _tabManager.ActiveTabChanged += OnActiveTabChanged;
        Configuration.AiSettings.ActiveProviderChanged += OnAiEnabledChanged;

        Loaded += WorkspacePage_Loaded;
        Unloaded += WorkspacePage_Unloaded;
    }

    private async void WorkspacePage_Loaded(object sender, RoutedEventArgs e) => await LoadNotes();

    private void WorkspacePage_Unloaded(object sender, RoutedEventArgs e)
    {
        _summaryCts?.Cancel();
        _summaryCts?.Dispose();
        Configuration.AiSettings.ActiveProviderChanged -= OnAiEnabledChanged;
        GraphView.Cleanup();
    }

    private void OnAiEnabledChanged() =>
        DispatcherQueue.TryEnqueue(async () =>
        {
            await _mediator.Send(new InitializeWorkspaceCommand());
            if (GraphView.Visibility is Visibility.Visible)
            {
                List<Note> allNotes = [.. await _mediator.Send(new GetNotesQueryRequest())];
                await GraphView.LoadGraphAsync(allNotes);
            }
        });

    private async Task LoadNotes()
    {
        await _mediator.Send(new InitializeWorkspaceCommand());
        var workspace = await _mediator.Send(new LoadWorkspaceQueryRequest());
        Sidebar.SetVaultName(workspace.VaultName);

        if (workspace.RootFolder is not null)
            Sidebar.LoadFolders(workspace.RootFolder, workspace.FavoritesSection);

        if (workspace.InitialNoteFilePath is not null)
        {
            List<Note> allNotes = [.. await _mediator.Send(new GetNotesQueryRequest())];
            var activeNote = allNotes.FirstOrDefault(n => n.FilePath is var fp && fp == workspace.InitialNoteFilePath);
            if (activeNote is not null)
                SetSelectedNote(activeNote);
        }
    }

    private async Task ResetWorkspaceAsync()
    {
        Sidebar.ClearSearch();
        _tabManager.Tabs.Clear();
        SetSelectedNote(null);
        await LoadNotes();
    }

    private void SetSelectedNote(Note? note)
    {
        _selectedNote = note;
        UpdateEditorState();
    }

    private async void UpdateEditorState()
    {
        GraphView.Visibility = Visibility.Collapsed;

        var showEditor = _selectedNote is not null && _tabManager.ActiveTab?.IsNewTab is not true;
        NewTabView.Visibility = showEditor ? Visibility.Collapsed : Visibility.Visible;
        EditorView.Visibility = showEditor ? Visibility.Visible : Visibility.Collapsed;

        if (!showEditor) return;

        try
        {
            _pendingTitleRename = null;
            _summaryCts?.Cancel();
            EditorView.HideAiSummary();
            _isLoading = true;
            EditorView.SetTitle(_selectedNote!.Filename);
            await EditorView.SetContentAsync(_selectedNote.Text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update editor state");
        }
        finally
        {
            _isLoading = false;
        }
    }
}
