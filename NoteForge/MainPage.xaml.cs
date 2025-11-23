using NoteForge.Services;
using NoteForge.Models;

namespace NoteForge;

public partial class MainPage : ContentPage
{
    private readonly INoteService _noteService;
    private Note? _selectedNote;
    private CancellationTokenSource? _saveCts;
    private bool _isLoading;
    private bool _isSyncingTitle;

    public MainPage(INoteService noteService)
    {
        InitializeComponent();
        _noteService = noteService;
        Loaded += MainPage_Loaded;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
#if WINDOWS
        SetTitleBar();
#endif
    }

    private async void MainPage_Loaded(object? sender, EventArgs e)
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
            // Reset selection
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
            _selectedNote = selectedNote;
            UpdateEditorState();
        }
    }

    private void UpdateEditorState()
    {
        if (_selectedNote is null)
        {
            EmptyStateView.IsVisible = true;
            EditorView.IsVisible = false;
            TopTitleEntry.Text = "";
            TopTitleEntry.IsVisible = false;
        }
        else
        {
            _isLoading = true;
            EmptyStateView.IsVisible = false;
            EditorView.IsVisible = true;
            TopTitleEntry.IsVisible = true;

            var title = Path.GetFileNameWithoutExtension(_selectedNote.Filename);
            NoteTitleEntry.Text = title;
            TopTitleEntry.Text = title;

            NoteContentEditor.Text = _selectedNote.Text;
            _isLoading = false;
        }
    }

    private void OnTopTitleTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || _isSyncingTitle) return;
        SyncTitles(e.NewTextValue);
    }

    private void OnNoteTitleTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || _isSyncingTitle) return;
        SyncTitles(e.NewTextValue);
    }

    private void SyncTitles(string title)
    {
        _isSyncingTitle = true;
        if (TopTitleEntry.Text != title) TopTitleEntry.Text = title;
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
            // Refresh sidebar to show new name
            await LoadNotes();

            // Reselect the note
            var note = (NotesCollection.ItemsSource as IEnumerable<Note>)?.FirstOrDefault(n => n.FilePath == _selectedNote.FilePath);
            if (note is not null)
            {
                NotesCollection.SelectedItem = note;
                _selectedNote = note;
            }
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
            }
        }
        catch (TaskCanceledException)
        {
            // Debounce cancelled, new keystroke happened
        }
    }

#if WINDOWS
    private void OnMinimizeClicked(object sender, EventArgs e)
    {
        var window = this.Window.Handler.PlatformView as Microsoft.UI.Xaml.Window;
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        if (appWindow is not null)
        {
            (appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter)?.Minimize();
        }
    }

    private void SetTitleBar()
    {
        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
            var nativeWindow = handler.PlatformView;
            nativeWindow.Activate();

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow is not null)
            {
                nativeWindow.ExtendsContentIntoTitleBar = true;
                nativeWindow.SetTitleBar(AppTitleBar.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement);
            }
        });
    }
#endif

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }
}