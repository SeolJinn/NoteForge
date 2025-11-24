using NoteForge.Services;
using NoteForge.Models;
using Tab = NoteForge.Models.Tab;

namespace NoteForge.Views;

public partial class WorkspacePage : ContentPage
{
    private readonly INoteService _noteService;
    private readonly ITabManager _tabManager;
    private Note? _selectedNote;
    private CancellationTokenSource? _saveCts;
    private bool _isLoading;
    private bool _isSyncingTitle;

    public WorkspacePage(INoteService noteService, ITabManager tabManager)
    {
        InitializeComponent();
        _noteService = noteService;
        _tabManager = tabManager;

        // Bind Tabs
        TabsCollection.ItemsSource = _tabManager.Tabs;
        
        // Subscribe to ActiveTabChanged
        _tabManager.ActiveTabChanged += OnActiveTabChanged;

        Loaded += WorkspacePage_Loaded;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }

    private async void WorkspacePage_Loaded(object? sender, EventArgs e)
    {
        await LoadNotes();
    }

    private async Task LoadNotes()
    {
        // Ensure we have a configured path
        if (!_noteService.IsConfigured)
        {
            PathLabel.Text = "No vault selected";
            NotesCollection.ItemsSource = null;
            return;
        }

        PathLabel.Text = $"Path: {_noteService.CurrentNotebookPath}";
        var notes = await _noteService.GetNotesAsync();

        NotesCollection.ItemsSource = notes.OrderByDescending(n => n.Date);
    }

    private async void OnChangeFolderClicked(object sender, EventArgs e)
    {
        var newPath = await _noteService.PickFolderAsync();
        if (newPath is not null)
        {
            await LoadNotes();
            // Close all tabs on vault switch? Or keep them?
            // Usually better to close or reset.
            // For now, let's clear tabs.
            _tabManager.Tabs.Clear();
            _selectedNote = null;
            UpdateEditorState();
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadNotes();
    }

    private void OnToggleSidebarClicked(object sender, EventArgs e)
    {
        Sidebar.IsVisible = !Sidebar.IsVisible;
    }

    private void OnNoteSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Note selectedNote)
        {
            _tabManager.OpenTab(selectedNote);
            
            // We don't need to set _selectedNote here directly, 
            // ActiveTabChanged will handle it.
            
            // Clear sidebar selection to allow re-clicking if needed, 
            // but keeping it selected shows context. 
            // However, since we have tabs, the sidebar selection is less critical for "active state".
            NotesCollection.SelectedItem = null;
        }
    }

    private void OnTabSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Tab tab)
        {
            _tabManager.ActivateTab(tab);
        }
    }

    private void OnCloseTabClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Tab tab)
        {
            _tabManager.CloseTab(tab);
        }
    }

    private void OnActiveTabChanged(object? sender, Tab? activeTab)
    {
        if (activeTab == null)
        {
             TabsCollection.SelectedItem = null;
             _selectedNote = null;
             UpdateEditorState();
        }
        else
        {
             TabsCollection.SelectedItem = activeTab;
             
             // Find the note corresponding to this tab
             // We iterate through the loaded notes.
             var notes = NotesCollection.ItemsSource as IEnumerable<Note>;
             var note = notes?.FirstOrDefault(n => n.FilePath == activeTab.FilePath);

             if (note != null)
             {
                 _selectedNote = note;
                 UpdateEditorState();
             }
             else
             {
                 // Note might not be in the list (deleted? or we just haven't refreshed)
                 // For now, we can try to reload notes or show error.
                 // Or maybe we should create a Note object if it exists on disk?
                 if (File.Exists(activeTab.FilePath))
                 {
                      _selectedNote = new Note 
                      { 
                          FilePath = activeTab.FilePath, 
                          Filename = Path.GetFileName(activeTab.FilePath),
                          Text = File.ReadAllText(activeTab.FilePath),
                          Date = File.GetLastWriteTime(activeTab.FilePath)
                      };
                      UpdateEditorState();
                 }
                 else
                 {
                     // File missing
                     _tabManager.CloseTab(activeTab);
                 }
             }
        }
    }

    private void UpdateEditorState()
    {
        if (_selectedNote is null)
        {
            EmptyStateView.IsVisible = true;
            EditorView.IsVisible = false;
        }
        else
        {
            _isLoading = true;
            EmptyStateView.IsVisible = false;
            EditorView.IsVisible = true;

            var title = Path.GetFileNameWithoutExtension(_selectedNote.Filename);
            NoteTitleEntry.Text = title;

            NoteContentEditor.Text = _selectedNote.Text;
            _isLoading = false;
        }
    }

    private void OnNoteTitleTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || _isSyncingTitle) return;
        SyncTitles(e.NewTextValue);
    }

    private void SyncTitles(string title)
    {
        _isSyncingTitle = true;
        if (NoteTitleEntry.Text != title) NoteTitleEntry.Text = title;
        _isSyncingTitle = false;
    }

    private async void OnTitleCompleted(object sender, EventArgs e)
    {
        if (sender is Entry entry)
            entry.Unfocus();

        await RenameCurrentNote();
    }

    private async void OnTitleUnfocused(object sender, FocusEventArgs e)
    {
        await RenameCurrentNote();
    }

    private async Task RenameCurrentNote()
    {
        if (_selectedNote is null) return;

        var newTitle = NoteTitleEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(newTitle))
        {
            // Revert if empty
            var oldTitle = Path.GetFileNameWithoutExtension(_selectedNote.Filename);
            SyncTitles(oldTitle);
            return;
        }

        var currentTitle = Path.GetFileNameWithoutExtension(_selectedNote.Filename);
        if (string.Equals(newTitle, currentTitle, StringComparison.OrdinalIgnoreCase)) return;

        var success = await _noteService.RenameNoteAsync(_selectedNote, newTitle);
        if (success)
        {
            // Update Tab
            if (_tabManager.ActiveTab != null)
            {
                _tabManager.ActiveTab.DisplayName = _selectedNote.Filename;
                _tabManager.ActiveTab.FilePath = _selectedNote.FilePath;
            }

            // Refresh sidebar to show new name
            await LoadNotes();
        }
        else
        {
            // Revert on failure (e.g. file exists)
            SyncTitles(currentTitle);
        }
    }

    private async void OnNoteContentChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || _selectedNote is null)
            return;

        // Mark tab as dirty
        if (_tabManager.ActiveTab != null)
        {
             _tabManager.SetDirty(_selectedNote.FilePath, true);
        }

        // Debounce: Cancel previous timer
        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        try
        {
            // Wait for 200ms of inactivity
            await Task.Delay(200, token);

            if (!token.IsCancellationRequested)
            {
                _selectedNote.Text = NoteContentEditor.Text;
                await _noteService.SaveNoteAsync(_selectedNote);
                
                // Mark tab as clean
                if (_tabManager.ActiveTab != null)
                {
                     _tabManager.SetDirty(_selectedNote.FilePath, false);
                }
            }
        }
        catch (TaskCanceledException)
        {
            // Debounce cancelled, new keystroke happened
        }
    }
}
