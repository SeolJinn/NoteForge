using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Models;
using NoteForge.Services;

namespace NoteForge.Views;

public sealed partial class WorkspacePage : Page
{
    private readonly INoteService _noteService;
    private readonly ITabManager _tabManager;
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

        TabsCollection.ItemsSource = _tabManager.Tabs;
        
        _tabManager.ActiveTabChanged += OnActiveTabChanged;

        this.Loaded += WorkspacePage_Loaded;
    }

    private async void WorkspacePage_Loaded(object sender, RoutedEventArgs e)
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

        var sortedNotes = notes.OrderByDescending(n => n.Date).ToList();
        NotesCollection.ItemsSource = sortedNotes;

        if (_tabManager.ActiveTab is not null)
        {
            var activeNote = sortedNotes.FirstOrDefault(n => n.FilePath == _tabManager.ActiveTab.FilePath);
            if (activeNote != null)
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
                n.IsSelected = (n == _selectedNote || (n.FilePath == _selectedNote?.FilePath));
            }
        }

        UpdateEditorState();
    }

    private async void OnChangeFolderClicked(object sender, RoutedEventArgs e)
    {
        var newPath = await _noteService.PickFolderAsync();
        if (newPath is not null)
        {
            await LoadNotes();
            _tabManager.Tabs.Clear();
            SetSelectedNote(null);
        }
    }

    private async void OnRefreshClicked(object sender, RoutedEventArgs e)
    {
        await LoadNotes();
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
        DispatcherQueue.TryEnqueue(() =>
        {
            if (activeTab is null)
            {
                 TabsCollection.SelectedItem = null;
                 NotesCollection.SelectedItem = null;
                 SetSelectedNote(null);
            }
            else
            {
                 TabsCollection.SelectedItem = activeTab;
                 
                 var notes = NotesCollection.ItemsSource as IEnumerable<Note>;
                 var note = notes?.FirstOrDefault(n => n.FilePath == activeTab.FilePath);

                 if (note is not null)
                 {
                     SetSelectedNote(note);
                     NotesCollection.SelectedItem = note;
                 }
                 else
                 {
                     if (File.Exists(activeTab.FilePath))
                     {
                          var newNote = new Note 
                          { 
                              FilePath = activeTab.FilePath, 
                              Filename = Path.GetFileNameWithoutExtension(activeTab.FilePath),
                              Text = File.ReadAllText(activeTab.FilePath),
                              Date = File.GetLastWriteTime(activeTab.FilePath)
                          };
                          SetSelectedNote(newNote);
                     }
                     else
                     {
                         _tabManager.CloseTab(activeTab);
                     }
                 }
            }
        });
    }

    private void UpdateEditorState()
    {
        if (_selectedNote is null)
        {
            EmptyStateView.Visibility = Visibility.Visible;
            EditorView.Visibility = Visibility.Collapsed;
        }
        else
        {
            _isLoading = true;
            EmptyStateView.Visibility = Visibility.Collapsed;
            EditorView.Visibility = Visibility.Visible;

            var title = _selectedNote.Filename;
            NoteTitleEntry.Text = title;

            NoteContentEditor.Text = _selectedNote.Text;
            _isLoading = false;
        }
    }

    private void OnNoteTitleTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || _isSyncingTitle) return;
        
        _renameCts?.Cancel();
        _renameCts = new CancellationTokenSource();
        var token = _renameCts.Token;

        Task.Delay(200, token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            
            DispatcherQueue.TryEnqueue(async () =>
            {
                if (token.IsCancellationRequested) return;
                await RenameCurrentNote();
            });
        });
    }

    private void SyncTitles(string title)
    {
        _isSyncingTitle = true;
        if (NoteTitleEntry.Text != title) NoteTitleEntry.Text = title;
        _isSyncingTitle = false;
    }

    private async void OnTitleUnfocused(object sender, RoutedEventArgs e)
    {
        _renameCts?.Cancel();
        await RenameCurrentNote();
    }

    private async Task RenameCurrentNote()
    {
        if (_selectedNote is null) return;

        var newTitle = NoteTitleEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(newTitle))
        {
            var oldTitle = _selectedNote.Filename;
            SyncTitles(oldTitle);
            return;
        }

        var currentTitle = _selectedNote.Filename;
        if (string.Equals(newTitle, currentTitle, StringComparison.OrdinalIgnoreCase)) return;

        var success = await _noteService.RenameNoteAsync(_selectedNote, newTitle);
        if (success)
        {
            if (_tabManager.ActiveTab is not null)
            {
                _tabManager.ActiveTab.DisplayName = _selectedNote.Filename;
                _tabManager.ActiveTab.FilePath = _selectedNote.FilePath;
            }
        }
        else
        {
            SyncTitles(currentTitle);
        }
    }

    private async void OnNoteContentChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || _selectedNote is null)
            return;

        if (_tabManager.ActiveTab is not null)
        {
             _tabManager.SetDirty(_selectedNote.FilePath, true);
        }

        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        try
        {
            await Task.Delay(200, token);

            if (!token.IsCancellationRequested)
            {
                _selectedNote.Text = NoteContentEditor.Text;
                await _noteService.SaveNoteAsync(_selectedNote);
                
                if (_tabManager.ActiveTab is not null)
                {
                     _tabManager.SetDirty(_selectedNote.FilePath, false);
                }
            }
        }
        catch (TaskCanceledException)
        {
        }
    }
}

