using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using NoteForge.Interfaces;
using NoteForge.Handlers;
using NoteForge.Models;

namespace NoteForge.Views;

public sealed partial class WorkspacePage : Page
{
    private readonly INoteService _noteService;
    private readonly ITabManager _tabManager;
    private readonly IMarkdownPreviewService _previewService;
    private readonly IMediator _mediator;
    private Note? _selectedNote;
    private CancellationTokenSource? _saveCts;
    private CancellationTokenSource? _renameCts;
    private bool _isLoading;
    private bool _isSyncingTitle;

    public WorkspacePage()
    {
        InitializeComponent();
        _noteService = App.NoteService;
        _tabManager = App.TabManager;
        _previewService = App.PreviewService;
        _mediator = App.Mediator;

        TabsCollection.ItemsSource = _tabManager.Tabs;
        
        _tabManager.ActiveTabChanged += OnActiveTabChanged;

        this.Loaded += WorkspacePage_Loaded;
    }

    private async void WorkspacePage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateVaultName();
        await LoadNotes();
    }

    private void UpdateVaultName()
    {
        CurrentVaultName.Text = _noteService.IsConfigured 
            ? _noteService.CurrentVaultName 
            : "No vault selected";
    }

    private async Task LoadNotes()
    {
        if (!_noteService.IsConfigured)
        {
            PathLabel.Text = "No vault selected";
            NotesCollection.ItemsSource = null;
            return;
        }

        PathLabel.Text = $"Path: {_noteService.CurrentNotebookPath}";
        var sortedNotes = (await _mediator.Send(new GetNotesQueryRequest())).ToList();
        NotesCollection.ItemsSource = sortedNotes;

        if (_tabManager.Tabs.Count == 0)
        {
            _tabManager.OpenNewTab();
        }
        else if (_tabManager.ActiveTab is { IsNewTab: false })
        {
            var activeNote = sortedNotes.FirstOrDefault(n => n.FilePath == _tabManager.ActiveTab.FilePath);
            if (activeNote is not null)
            {
                SetSelectedNote(activeNote);
                NotesCollection.SelectedItem = activeNote;
            }
        }
    }

    private void SetSelectedNote(Note? note)
    {
        _selectedNote = note;

        if (NotesCollection.ItemsSource is IEnumerable<Note> notes)
        {
            foreach (var n in notes)
            {
                n.IsSelected = n == _selectedNote || (n.FilePath == _selectedNote?.FilePath);
            }
        }

        UpdateEditorState();
    }

    private void OnVaultSelectorClicked(object sender, RoutedEventArgs e)
    {
         //Flyout opens automatically via Button.Flyout
    }

    private void OnVaultFlyoutOpening(object sender, object e)
    {
        VaultDropdownIcon.Glyph = "\uE70E"; // Chevron up
        var vaults = _noteService.GetRecentVaults();
        VaultsList.ItemsSource = vaults;
    }

    private void OnVaultFlyoutClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        VaultDropdownIcon.Glyph = "\uE70D"; // Chevron down
    }

    private void OnVaultSelected(object sender, SelectionChangedEventArgs e)
    {
        VaultsList.SelectedItem = null;
    }

    private async void OnVaultItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is VaultInfo selectedVault)
        {
            VaultFlyout.Hide();

            if (Directory.Exists(selectedVault.Path))
            {
                _noteService.SetVaultPath(selectedVault.Path);
                _tabManager.Tabs.Clear();
                SetSelectedNote(null);
                UpdateVaultName();
                await LoadNotes();
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "Vault folder no longer exists.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
    }

    private void OnManageVaultsClicked(object sender, RoutedEventArgs e)
    {
        VaultFlyout.Hide();
        
        var vaultWindow = new VaultManagerWindow();
        vaultWindow.VaultSelected += (s, path) =>
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                _tabManager.Tabs.Clear();
                SetSelectedNote(null);
                UpdateVaultName();
                await LoadNotes();
            });
        };
        vaultWindow.Activate();
    }

    private void OnToggleSidebarClicked(object sender, RoutedEventArgs e)
    {
        Sidebar.Visibility = Sidebar.Visibility is Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        TitleBarSidebarColumn.Width = Sidebar.Visibility is Visibility.Visible ? new GridLength(250) : GridLength.Auto;
    }

    private void OnNoteSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is Note selectedNote)
        {
            if (_tabManager.ActiveTab?.FilePath == selectedNote.FilePath)
            {
                SetSelectedNote(selectedNote);
                return;
            }
            _tabManager.OpenTab(selectedNote);
        }
    }

    private void OnTabSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is Tab tab)
        {
            _tabManager.ActivateTab(tab);
        }
    }

    private void OnCloseTabClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Tab tab)
        {
            _tabManager.CloseTab(tab);
        }
    }

    private void OnActiveTabChanged(object? sender, Tab? activeTab)
    {
        DispatcherQueue.TryEnqueue(async () => await HandleActiveTabChangedAsync(activeTab));
    }

    private async Task HandleActiveTabChangedAsync(Tab? activeTab)
    {
        TabsCollection.SelectedItem = activeTab;

        if (activeTab is null or { IsNewTab: true })
        {
            NotesCollection.SelectedItem = null;
            SetSelectedNote(null);
            if (activeTab?.IsNewTab is true) UpdateEditorState();
            return;
        }

        var notes = NotesCollection.ItemsSource as IEnumerable<Note>;
        var note = notes?.FirstOrDefault(n => n.FilePath == activeTab.FilePath);

        if (note is not null)
        {
            SetSelectedNote(note);
            NotesCollection.SelectedItem = note;
        }
        else
        {
            var loadedNote = await _mediator.Send(new GetNoteByPathQueryRequest(activeTab.FilePath));
            if (loadedNote is not null)
            {
                SetSelectedNote(loadedNote);
            }
            else
            {
                _tabManager.CloseTab(activeTab);
            }
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

        _isLoading = true;
        NoteTitleEntry.Text = _selectedNote!.Filename;
        NoteContentEditor.Text = _selectedNote.Text;
        _isLoading = false;
        UpdatePreviewAsync();
    }

    private async void UpdatePreviewAsync()
    {
        if (_selectedNote is null || PreviewColumn.Width.Value == 0) 
        {
            return;
        }

        try
        {
            if (PreviewWebView.CoreWebView2 is null) await PreviewWebView.EnsureCoreWebView2Async();
            var html = _previewService.ConvertToHtml(_selectedNote.Text ?? "");
            PreviewWebView.NavigateToString(_previewService.WrapInHtmlDocument(html));
        }
        catch { }
    }

    private void OnTogglePreviewClicked(object sender, RoutedEventArgs e)
    {
        var isHidden = PreviewColumn.Width.Value == 0;
        PreviewColumn.Width = isHidden ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        PreviewToggleBtn.Content = isHidden ? ">" : "<";
        if (isHidden) UpdatePreviewAsync();
    }

    private void OnNoteTitleTextChanged(object sender, TextChangedEventArgs e)
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

                await RenameCurrentNote();
            });
        });
    }

    private void SyncTitles(string title)
    {
        _isSyncingTitle = true;
        if (NoteTitleEntry.Text != title) 
        {
            NoteTitleEntry.Text = title;
        }
        _isSyncingTitle = false;
    }

    private async void OnTitleUnfocused(object sender, RoutedEventArgs e)
    {
        _renameCts?.Cancel();
        await RenameCurrentNote();
    }

    private async Task RenameCurrentNote()
    {
        if (_selectedNote is null) 
        {
            return;
        }

        var newTitle = NoteTitleEntry.Text?.Trim();
        var currentTitle = _selectedNote.Filename;

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

    private async void OnNoteContentChanged(object sender, TextChangedEventArgs e)
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

            _selectedNote.Text = NoteContentEditor.Text;
            UpdatePreviewAsync();
            await _mediator.Send(new SaveNoteCommandRequest(_selectedNote), token);
            _tabManager.SetDirty(_selectedNote.FilePath, false);
        }
        catch (TaskCanceledException) { }
    }

    private void OnNewTabClicked(object sender, RoutedEventArgs e)
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
}