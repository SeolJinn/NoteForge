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
using NoteForge.Handlers.AI;
using NoteForge.Handlers.Folders;
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

        Loaded += WorkspacePage_Loaded;
        Unloaded += WorkspacePage_Unloaded;
    }

    private async void WorkspacePage_Loaded(object sender, RoutedEventArgs e) => await LoadNotes();

    private void WorkspacePage_Unloaded(object sender, RoutedEventArgs e)
    {
        _summaryCts?.Cancel();
        _summaryCts?.Dispose();
        GraphView.Cleanup();
    }

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
            var activeNote = allNotes.FirstOrDefault(n => n.FilePath == workspace.InitialNoteFilePath);
            if (activeNote is not null)
                SetSelectedNote(activeNote);
        }
    }

    private void SetSelectedNote(Note? note)
    {
        _selectedNote = note;
        UpdateEditorState();
    }

    private async Task ResetWorkspaceAsync()
    {
        Sidebar.ClearSearch();
        _tabManager.Tabs.Clear();
        SetSelectedNote(null);
        await LoadNotes();
    }

    private async void OnVaultItemClicked(object sender, VaultInfo selectedVault)
    {
        var result = await _mediator.Send(new SwitchVaultCommandRequest(selectedVault.Path));
        if (result.Success)
        {
            await ResetWorkspaceAsync();
        }
        else
        {
            await _dialogService.ShowErrorAsync(result.ErrorMessage!, XamlRoot);
        }
    }

    private void OnManageVaultsClicked(object sender, EventArgs e)
    {
        var vaultWindow = new VaultManagerWindow();
        vaultWindow.VaultSelected += (s, path) 
            => DispatcherQueue.TryEnqueue(async () => await ResetWorkspaceAsync());
        vaultWindow.Activate();
    }

    private void OnToggleSidebarClicked(object sender, RoutedEventArgs e) =>
        _sidebarCoordinator.ToggleSidebar(Sidebar, SidebarColumn, TitleBarSidebarColumn, SplitterBorder);

    private void OnFolderViewClicked(object sender, RoutedEventArgs e)
    {
        GraphView.Visibility = Visibility.Collapsed;
        _sidebarCoordinator.ShowFolderView(Sidebar, SidebarColumn, TitleBarSidebarColumn, SplitterBorder);
    }

    private async void OnSearchViewClicked(object sender, RoutedEventArgs e)
    {
        GraphView.Visibility = Visibility.Collapsed;
        List<Note> allNotes = [.. await _mediator.Send(new GetNotesQueryRequest())];
        _sidebarCoordinator.ShowSearchView(Sidebar, SidebarColumn, TitleBarSidebarColumn, SplitterBorder, allNotes);
    }

    private async void OnGraphViewClicked(object sender, RoutedEventArgs e)
    {
        EditorView.Visibility = Visibility.Collapsed;
        NewTabView.Visibility = Visibility.Collapsed;
        GraphView.Visibility = Visibility.Visible;

        List<Note> allNotes = [.. await _mediator.Send(new GetNotesQueryRequest())];
        await GraphView.LoadGraphAsync(allNotes);
    }

    private void OnGraphNodeClicked(object sender, Note note)
    {
        GraphView.Visibility = Visibility.Collapsed;

        _sidebarCoordinator.ShowFolderView(Sidebar, SidebarColumn, TitleBarSidebarColumn, SplitterBorder);

        _tabManager.OpenTab(note);
    }

    private void OnNoteSelected(object sender, Note selectedNote)
    {
        if (_tabManager.ActiveTab?.FilePath == selectedNote.FilePath)
            SetSelectedNote(selectedNote);
        else
            _tabManager.OpenTab(selectedNote);
    }

    private async void OnMatchingLineSelected(object sender, (Note Note, int LineNumber) args)
    {
        if (_tabManager.ActiveTab?.FilePath != args.Note.FilePath)
            _tabManager.OpenTab(args.Note);
        else
            SetSelectedNote(args.Note);

        await Task.Delay(100);
        await EditorView.NavigateToLineAsync(args.LineNumber);
    }

    private void OnTabSelected(object sender, Tab tab)
    {
        if (GraphView.Visibility is Visibility.Visible)
        {
            GraphView.Visibility = Visibility.Collapsed;
            UpdateEditorState();
        }

        _tabManager.ActivateTab(tab);
    }

    private void OnCloseTabClicked(object sender, Tab tab) => _tabManager.CloseTab(tab);

    private void OnActiveTabChanged(object? sender, Tab? activeTab) =>
        DispatcherQueue.TryEnqueue(async () => await HandleActiveTabChangedAsync(activeTab));

    private async Task HandleActiveTabChangedAsync(Tab? activeTab)
    {
        if (_selectedNote is not null && EditorView.IsEditorReady)
        {
            try
            {
                _selectedNote.Text = await EditorView.GetContentAsync(TimeSpan.FromSeconds(1));
                await _mediator.Send(new SaveNoteCommandRequest(_selectedNote));
                _tabManager.SetDirty(_selectedNote.FilePath, false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save outgoing tab content");
            }
        }

        TabBarControl.SetSelectedItem(activeTab);
        GraphView.Visibility = Visibility.Collapsed;

        if (activeTab is null or { IsNewTab: true })
        {
            Sidebar.SetSelectedNote(null);
            SetSelectedNote(null);
            if (activeTab?.IsNewTab is true) UpdateEditorState();
            return;
        }

        var loadedNote = await _mediator.Send(new GetNoteByPathQueryRequest(activeTab.FilePath));
        if (loadedNote is not null)
        {
            SetSelectedNote(loadedNote);
            Sidebar.SetSelectedNote(loadedNote);
        }
        else
            _tabManager.CloseTab(activeTab);
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

    private void OnNoteTitleTextChanged(object sender, string newTitle)
    {
        if (_isLoading || _isSyncingTitle) return;

        _pendingTitleRename = newTitle;
    }

    private void SyncTitles(string title)
    {
        _isSyncingTitle = true;
        EditorView.SetTitle(title);
        _isSyncingTitle = false;
    }

    private async void OnTitleUnfocused(object sender, EventArgs e)
    {
        if (_pendingTitleRename is not null)
        {
            await RenameCurrentNote(_pendingTitleRename);
            _pendingTitleRename = null;
        }
    }

    private async Task RenameCurrentNote(string? newTitle)
    {
        if (_selectedNote is null) return;

        var currentTitle = _selectedNote.Filename;
        newTitle = newTitle?.Trim();

        if (string.IsNullOrWhiteSpace(newTitle))
        {
            SyncTitles(currentTitle);
            return;
        }

        if (string.Equals(newTitle, currentTitle, StringComparison.OrdinalIgnoreCase)) return;

        var result = await _mediator.Send(new RenameNoteCommandRequest(_selectedNote, newTitle));
        if (result.Success && _tabManager.ActiveTab is not null)
        {
            _tabManager.ActiveTab.DisplayName = _selectedNote.Filename;
            _tabManager.ActiveTab.FilePath = _selectedNote.FilePath;
            await LoadNotes();
        }
        else if (!result.Success)
        {
            SyncTitles(currentTitle);
            Toast.Show(result.ErrorMessage!);
        }
    }

    private async void OnNoteContentChanged(object sender, EventArgs e)
    {
        if (_isLoading || _selectedNote is null) return;

        _tabManager.SetDirty(_selectedNote.FilePath, true);

        var noteToSave = _selectedNote;
        try
        {
            noteToSave.Text = await EditorView.GetContentAsync();
            await _mediator.Send(new SaveNoteCommandRequest(noteToSave));
            _tabManager.SetDirty(noteToSave.FilePath, false);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Failed to get content for save");
        }
    }

    private void OnNewTabClicked(object sender, EventArgs e) => _tabManager.OpenNewTab();

    private async void OnCreateNewNoteClicked(object sender, RoutedEventArgs e)
    {
        var newNote = await _mediator.Send(new CreateNoteCommandRequest());
        if (newNote is not null)
        {
            await LoadNotes();
            _tabManager.OpenTab(newNote);
        }
        else
        {
            Toast.Show("Failed to create a new note.");
        }
    }

    private async void OnGoToFileClicked(object sender, RoutedEventArgs e)
    {
        if (_quickFileNavigator is null)
        {
            _quickFileNavigator = new QuickFileNavigator();
            _quickFileNavigator.NoteSelected += (s, note) => _tabManager.OpenTab(note);
        }

        List<Note> allNotes = [.. await _mediator.Send(new GetNotesQueryRequest())];
        _quickFileNavigator.Show(allNotes, XamlRoot);
    }

    private async void OnToggleFavoriteClicked(object sender, Note note)
    {
        await _mediator.Send(new ToggleFavoriteCommandRequest(note));
        await LoadNotes();
    }

    private async void OnCreateFolderClicked(object sender, Folder? folder)
    {
        var folderName = await _folderDialogService.ShowCreateFolderDialogAsync(XamlRoot);
        if (folderName is not null)
        {
            var folderPath = folder?.DirectoryPath ?? _noteService.CurrentNotebookPath;
            var result = await _mediator.Send(new CreateFolderCommandRequest(folderPath, folderName));
            if (result.Success)
                await LoadNotes();
            else
                Toast.Show(result.ErrorMessage!);
        }
    }

    private async void OnRenameFolderClicked(object sender, Folder folder)
    {
        var newName = await _folderDialogService.ShowRenameFolderDialogAsync(folder.Name, XamlRoot);
        if (newName is not null)
        {
            var result = await _mediator.Send(new RenameFolderCommandRequest(folder.DirectoryPath, newName));
            if (result.Success)
                await LoadNotes();
            else
                Toast.Show(result.ErrorMessage!);
        }
    }

    private async void OnDeleteFolderClicked(object sender, Folder folder)
    {
        if (!folder.IsEmpty)
        {
            Toast.Show("Cannot delete a non-empty folder.");
            return;
        }

        var confirmed = await _folderDialogService.ShowDeleteFolderDialogAsync(folder.Name, XamlRoot);
        if (confirmed)
        {
            var result = await _mediator.Send(new DeleteFolderCommandRequest(folder.DirectoryPath));
            if (result.Success)
                await LoadNotes();
            else
                Toast.Show(result.ErrorMessage!);
        }
    }

    private async void OnNoteMovedToFolder(object sender, (Note Note, Folder TargetFolder) e)
    {
        var wasSelected = _selectedNote?.FilePath == e.Note.FilePath;
        var expandedStates = Sidebar.GetFolderExpandedStates();

        var result = await _mediator.Send(new MoveNoteToFolderCommandRequest(e.Note, e.TargetFolder.DirectoryPath));
        if (!result.Success)
        {
            Toast.Show(result.ErrorMessage!);
            return;
        }

        await LoadNotes();

        Sidebar.RestoreFolderExpandedStates(expandedStates);

        if (wasSelected && _selectedNote is not null)
        {
            Sidebar.SetSelectedNote(_selectedNote);
        }
    }

    private async void OnGenerateSummaryClicked(object sender, EventArgs e)
    {
        if (_selectedNote is null || string.IsNullOrWhiteSpace(_selectedNote.Text)) return;

        _summaryCts?.Cancel();
        _summaryCts = new CancellationTokenSource();

        await _mediator.Send(new GenerateInlineAiSummaryCommandRequest(_selectedNote,
            OnSummaryStarted: () => { EditorView.ShowAiSummary("Generating summary..."); EditorView.SetSummaryButtonEnabled(false); },
            OnFirstToken: () => EditorView.SetAiSummaryText(string.Empty),
            OnTokenReceived: EditorView.AppendAiSummaryText,
            OnSummaryCompleted: () => EditorView.SetSummaryButtonEnabled(true),
            OnSummaryCancelled: () => { EditorView.SetAiSummaryText("Summary generation cancelled."); EditorView.SetSummaryButtonEnabled(true); },
            OnSummaryFailed: error => { EditorView.SetAiSummaryText($"Error: {error}"); EditorView.SetSummaryButtonEnabled(true); }
        ), _summaryCts.Token);
    }

    private void OnCloseSummaryClicked(object sender, EventArgs e)
    {
        _summaryCts?.Cancel();
        EditorView.HideAiSummary();
    }

    private async void OnEditorLinkClicked(object sender, string href)
    {
        if (href.StartsWith("[[") && href.EndsWith("]]"))
        {
            var noteName = href[2..^2];
            List<Note> allNotes = [.. await _mediator.Send(new GetNotesQueryRequest())];
            var targetNote = allNotes.FirstOrDefault(n =>
                n.Filename.Equals(noteName, StringComparison.OrdinalIgnoreCase));
            if (targetNote is not null)
                _tabManager.OpenTab(targetNote);
        }
        else if (Uri.TryCreate(href, UriKind.Absolute, out var uri))
        {
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }

    private async void OnEditorSaveRequested(object sender, EventArgs e)
    {
        if (_selectedNote is null) return;
        try
        {
            _selectedNote.Text = await EditorView.GetContentAsync();
            await _mediator.Send(new SaveNoteCommandRequest(_selectedNote));
            _tabManager.SetDirty(_selectedNote.FilePath, false);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Failed to save on Ctrl+S");
        }
    }
}