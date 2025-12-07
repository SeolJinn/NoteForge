using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using NoteForge.Handlers;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Views;

public sealed partial class WorkspacePage : Page
{
    private readonly INoteService _noteService;
    private readonly ITabManager _tabManager;
    private readonly IMarkdownPreviewService _previewService;
    private readonly IDialogService _dialogService;
    private readonly IMediator _mediator;
    private readonly ILogger<WorkspacePage> _logger;
    private Note? _selectedNote;
    private CancellationTokenSource? _saveCts;
    private CancellationTokenSource? _renameCts;
    private CancellationTokenSource? _summaryCts;
    private bool _isLoading;
    private bool _isSyncingTitle;

    public WorkspacePage()
    {
        InitializeComponent();
        _noteService = App.NoteService;
        _tabManager = App.TabManager;
        _previewService = App.PreviewService;
        _dialogService = App.DialogService;
        _mediator = App.Mediator;
        _logger = App.LoggerFactory.CreateLogger<WorkspacePage>();

        TabBarControl.SetItemsSource(_tabManager.Tabs);

        _tabManager.ActiveTabChanged += OnActiveTabChanged;

        Loaded += WorkspacePage_Loaded;
    }

    private async void WorkspacePage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadNotes();
    }

    private async Task LoadNotes()
    {
        var workspace = await _mediator.Send(new LoadWorkspaceQueryRequest());

        Sidebar.SetVaultName(workspace.VaultName);
        Sidebar.SetPathLabel(workspace.VaultPath);
        Sidebar.SetNotesSource(workspace.Notes);

        if (workspace.InitialNoteFilePath is not null)
        {
            var activeNote = workspace.Notes.FirstOrDefault(n => n.FilePath == workspace.InitialNoteFilePath);
            if (activeNote is not null)
            {
                SetSelectedNote(activeNote);
                Sidebar.SetSelectedNote(activeNote);
            }
        }
    }

    private void SetSelectedNote(Note? note)
    {
        _selectedNote = note;
        UpdateEditorState();
    }

    private async Task ResetWorkspaceAsync()
    {
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
        vaultWindow.VaultSelected += (s, path) =>
        {
            DispatcherQueue.TryEnqueue(async () => await ResetWorkspaceAsync());
        };
        vaultWindow.Activate();
    }

    private void OnToggleSidebarClicked(object sender, RoutedEventArgs e)
    {
        Sidebar.Visibility = Sidebar.Visibility is Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        TitleBarSidebarColumn.Width = Sidebar.Visibility is Visibility.Visible ? new GridLength(250) : GridLength.Auto;
    }

    private void OnNoteSelected(object sender, Note selectedNote)
    {
        if (_tabManager.ActiveTab?.FilePath == selectedNote.FilePath)
        {
            SetSelectedNote(selectedNote);
            return;
        }
        _tabManager.OpenTab(selectedNote);
    }

    private void OnTabSelected(object sender, Tab tab)
    {
        _tabManager.ActivateTab(tab);
    }

    private void OnCloseTabClicked(object sender, Tab tab)
    {
        _tabManager.CloseTab(tab);
    }

    private void OnActiveTabChanged(object? sender, Tab? activeTab)
    {
        DispatcherQueue.TryEnqueue(async () => await HandleActiveTabChangedAsync(activeTab));
    }

    private async Task HandleActiveTabChangedAsync(Tab? activeTab)
    {
        TabBarControl.SetSelectedItem(activeTab);

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
        {
            _tabManager.CloseTab(activeTab);
        }
    }

    private void UpdateEditorState()
    {
        var showEditor = _selectedNote is not null && _tabManager.ActiveTab?.IsNewTab != true;
        NewTabView.Visibility = showEditor ? Visibility.Collapsed : Visibility.Visible;
        EditorView.Visibility = showEditor ? Visibility.Visible : Visibility.Collapsed;

        if (!showEditor)
        {
            return;
        }

        _summaryCts?.Cancel();
        EditorView.HideAiSummary();

        _isLoading = true;
        EditorView.SetTitle(_selectedNote!.Filename);
        EditorView.SetContent(_selectedNote.Text);
        _isLoading = false;
        UpdatePreviewAsync();
    }

    private async void UpdatePreviewAsync()
    {
        if (_selectedNote is null || EditorView.GetPreviewColumnWidth() == 0)
        {
            return;
        }

        try
        {
            var previewWebView = EditorView.GetPreviewWebView();
            if (previewWebView.CoreWebView2 is null) await previewWebView.EnsureCoreWebView2Async();
            var html = _previewService.ConvertToHtml(_selectedNote.Text ?? "");
            previewWebView.NavigateToString(_previewService.WrapInHtmlDocument(html));
        }
        catch { }
    }

    private void OnTogglePreviewClicked(object sender, EventArgs e)
    {
        var isHidden = EditorView.GetPreviewColumnWidth() == 0;
        EditorView.SetPreviewColumnWidth(isHidden ? 1 : 0);
        if (isHidden) UpdatePreviewAsync();
    }

    private void OnNoteTitleTextChanged(object sender, string newTitle)
    {
        if (_isLoading || _isSyncingTitle)
        {
            return;
        }

        _renameCts?.Cancel();
        _renameCts = new CancellationTokenSource();
        var token = _renameCts.Token;

        Task.Delay(200, token).ContinueWith(t =>
        {
            if (t.IsCanceled)
            {
                return;
            }

            DispatcherQueue.TryEnqueue(async () =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await RenameCurrentNote(newTitle);
            });
        });
    }

    private void SyncTitles(string title)
    {
        _isSyncingTitle = true;
        EditorView.SetTitle(title);
        _isSyncingTitle = false;
    }

    private async void OnTitleUnfocused(object sender, EventArgs e)
    {
        _renameCts?.Cancel();
        await RenameCurrentNote(null);
    }

    private async Task RenameCurrentNote(string? newTitle)
    {
        if (_selectedNote is null)
        {
            return;
        }

        var currentTitle = _selectedNote.Filename;
        newTitle = newTitle?.Trim();

        if (string.IsNullOrWhiteSpace(newTitle))
        {
            SyncTitles(currentTitle);
            return;
        }

        if (string.Equals(newTitle, currentTitle, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (await _mediator.Send(new RenameNoteCommandRequest(_selectedNote, newTitle)) && _tabManager.ActiveTab is not null)
        {
            _tabManager.ActiveTab.DisplayName = _selectedNote.Filename;
            _tabManager.ActiveTab.FilePath = _selectedNote.FilePath;
        }
        else SyncTitles(currentTitle);
    }

    private async void OnNoteContentChanged(object sender, string newContent)
    {
        if (_isLoading || _selectedNote is null)
        {
            return;
        }

        _tabManager.SetDirty(_selectedNote.FilePath, true);
        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        try
        {
            await Task.Delay(200, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            _selectedNote.Text = newContent;
            UpdatePreviewAsync();
            await _mediator.Send(new SaveNoteCommandRequest(_selectedNote), token);
            _tabManager.SetDirty(_selectedNote.FilePath, false);
        }
        catch (TaskCanceledException) { }
    }

    private void OnNewTabClicked(object sender, EventArgs e)
    {
        _tabManager.OpenNewTab();
    }

    private async void OnCreateNewNoteClicked(object sender, RoutedEventArgs e)
    {
        var newNote = await _mediator.Send(new CreateNoteCommandRequest());
        if (newNote is not null)
        {
            await LoadNotes();
            _tabManager.OpenTab(newNote);
        }
    }

    private void OnGoToFileClicked(object sender, RoutedEventArgs e)
    {
        // TODO: Implement go to file functionality
    }

    private void OnAddToFavoritesClicked(object sender, EventArgs e)
    {
        // TODO: Implement Add to favorites functionality
    }

    private async void OnGenerateSummaryClicked(object sender, EventArgs e)
    {
        if (_selectedNote is null || string.IsNullOrWhiteSpace(_selectedNote.Text))
        {
            return;
        }

        _summaryCts?.Cancel();
        _summaryCts = new CancellationTokenSource();

        await _mediator.Send(new GenerateInlineAiSummaryCommandRequest(
            _selectedNote,
            OnSummaryStarted: () =>
            {
                EditorView.ShowAiSummary("Generating summary...");
                EditorView.SetSummaryButtonEnabled(false);
            },
            OnFirstToken: () => EditorView.SetAiSummaryText(string.Empty),
            OnTokenReceived: token => EditorView.AppendAiSummaryText(token),
            OnSummaryCompleted: () => EditorView.SetSummaryButtonEnabled(true),
            OnSummaryCancelled: () =>
            {
                EditorView.SetAiSummaryText("Summary generation cancelled.");
                EditorView.SetSummaryButtonEnabled(true);
            },
            OnSummaryFailed: error =>
            {
                EditorView.SetAiSummaryText($"Error: {error}");
                EditorView.SetSummaryButtonEnabled(true);
            }), _summaryCts.Token);
    }

    private void OnCloseSummaryClicked(object sender, EventArgs e)
    {
        _summaryCts?.Cancel();
        EditorView.HideAiSummary();
    }
}