using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Models;
using NoteForge.Services;
using NoteForge.Services.Search;

namespace NoteForge.Controls;

public enum SidebarViewMode
{
    Folder,
    Search
}

public sealed partial class WorkspaceSidebar : UserControl
{
    public event EventHandler<Note>? NoteSelected;
    public event EventHandler<(Note Note, int LineNumber)>? MatchingLineSelected;
    public event EventHandler<VaultInfo>? VaultSelected;
    public event EventHandler? ManageVaultsRequested;
    public event EventHandler<Folder?>? CreateFolderRequested;
    public event EventHandler<Folder>? RenameFolderRequested;
    public event EventHandler<Folder>? DeleteFolderRequested;
    public event EventHandler<(Note Note, Folder TargetFolder)>? NoteMovedToFolder;
    public event EventHandler<Note>? ToggleFavoriteRequested;

    private readonly FolderTreeService _folderTreeService;
    private readonly SemanticSearchStrategy _searchStrategy;
    private Folder? _rootFolder;
    private SectionView? _favoritesView;
    private List<Note> _allNotes = [];
    private SidebarViewMode _currentMode = SidebarViewMode.Folder;

    public WorkspaceSidebar()
    {
        InitializeComponent();
        _folderTreeService = App.Services.GetRequiredService<FolderTreeService>();
        _searchStrategy = App.Services.GetRequiredService<SemanticSearchStrategy>();
    }

    public void LoadFolders(Folder rootFolder, NoteSection? favoritesSection)
    {
        _rootFolder = rootFolder;
        FoldersContainer.Children.Clear();

        if (favoritesSection is not null && favoritesSection.Notes.Count > 0)
        {
            _favoritesView = new SectionView(favoritesSection);
            _favoritesView.NoteSelected += (s, note) => NoteSelected?.Invoke(this, note);
            _favoritesView.ToggleFavoriteRequested += (s, note) => ToggleFavoriteRequested?.Invoke(this, note);
            FoldersContainer.Children.Add(_favoritesView);
        }

        foreach (var folder in rootFolder.SubFolders)
        {
            var folderView = CreateFolderView(folder);
            FoldersContainer.Children.Add(folderView);
        }

        foreach (var note in rootFolder.Notes)
        {
            var noteItem = CreateRootNoteItem(note);
            FoldersContainer.Children.Add(noteItem);
        }
    }

    private FolderView CreateFolderView(Folder folder)
    {
        var folderView = new FolderView(folder, _rootFolder!);
        folderView.NoteSelected += (s, note) => NoteSelected?.Invoke(this, note);
        folderView.CreateSubfolderRequested += (s, f) => CreateFolderRequested?.Invoke(this, f);
        folderView.RenameFolderRequested += (s, f) => RenameFolderRequested?.Invoke(this, f);
        folderView.DeleteFolderRequested += (s, f) => DeleteFolderRequested?.Invoke(this, f);
        folderView.NoteMovedToFolder += (s, data) => NoteMovedToFolder?.Invoke(this, data);
        folderView.ToggleFavoriteRequested += (s, note) => ToggleFavoriteRequested?.Invoke(this, note);
        return folderView;
    }

    private Border CreateRootNoteItem(Note note)
    {
        var border = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(15, 10, 15, 10),
            Margin = new Thickness(10, 2, 10, 2),
            Tag = note
        };

        border.Tapped += (s, e) => NoteSelected?.Invoke(this, note);
        border.CanDrag = true;
        border.DragStarting += OnRootNoteDragStarting;

        var textBlock = new TextBlock
        {
            Text = note.Filename,
            FontSize = 14,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextPrimary"],
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var menuFlyout = new MenuFlyout();
        menuFlyout.Items.Add(CreateFlyoutItem("Toggle favorite", (s, e) 
            => ToggleFavoriteRequested?.Invoke(this, note)));

        border.ContextFlyout = menuFlyout;
        border.Child = textBlock;

        return border;
    }

    private MenuFlyoutItem CreateFlyoutItem(string text, RoutedEventHandler handler)
    {
        var item = new MenuFlyoutItem
        {
            Text = text,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextPrimary"]
        };
        item.Click += handler;
        return item;
    }

    public void LoadNotesForSearch(List<Note> notes)
    {
        _allNotes = notes;
        _searchStrategy.InvalidateIndex();
        _searchStrategy.InvalidateEmbeddingsCache();
    }

    public void SetViewMode(SidebarViewMode mode)
    {
        _currentMode = mode;

        if (mode is SidebarViewMode.Folder)
        {
            FoldersContainer.Visibility = Visibility.Visible;
            SearchView.Visibility = Visibility.Collapsed;
        }
        else
        {
            FoldersContainer.Visibility = Visibility.Collapsed;
            SearchView.Visibility = Visibility.Visible;
        }
    }

    public void SetVaultName(string name)
    {
        VaultSelectorControl.SetVaultName(name);
    }

    public void SetSelectedNote(Note? note)
    {
        _favoritesView?.SetSelectedNote(note);

        foreach (var child in FoldersContainer.Children)
        {
            if (child is FolderView folderView)
            {
                folderView.SetSelectedNote(note);
            }
            else if (child is Border border && border.Tag is Note rootNote)
            {
                border.Background = note is not null && rootNote.FilePath == note.FilePath
                    ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 52, 52, 52))
                    : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }
        }
    }

    private void OnSearchTextBoxLoaded(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Focus(FocusState.Programmatic);
    }

    private void OnSearchTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchTextBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            ShowSearchOptions();
            return;
        }

        ShowResults();
        var results = _searchStrategy.Search(_allNotes, query).First();
        SearchResultsControl.SetResults(results);
    }

    private void ShowSearchOptions()
    {
        SearchHintText.Visibility = Visibility.Visible;
        SearchResultsControl.Visibility = Visibility.Collapsed;
    }

    private void ShowResults()
    {
        SearchHintText.Visibility = Visibility.Collapsed;
        SearchResultsControl.Visibility = Visibility.Visible;
    }

    private void OnSearchResultNoteSelected(object sender, Note note)
    {
        NoteSelected?.Invoke(this, note);
    }

    private void OnSearchResultMatchingLineSelected(object sender, (Note Note, int LineNumber) data)
    {
        MatchingLineSelected?.Invoke(this, data);
    }

    private void OnVaultItemSelected(object sender, VaultInfo vaultInfo)
    {
        VaultSelected?.Invoke(this, vaultInfo);
    }

    private void OnManageVaultsRequested(object sender, EventArgs e)
    {
        ManageVaultsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCreateFolderClicked(object sender, RoutedEventArgs e)
    {
        CreateFolderRequested?.Invoke(this, null);
    }

    private void OnRootDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
    }

    private void OnRootDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("NoteFilePath", out var value) && value is string noteFilePath)
        {
            var note = _folderTreeService.FindNoteInTree(_rootFolder!, noteFilePath);
            if (note is not null)
            {
                NoteMovedToFolder?.Invoke(this, (Note: note, TargetFolder: _rootFolder!));
            }
        }
    }

    private void OnRootNoteDragStarting(UIElement sender, Microsoft.UI.Xaml.DragStartingEventArgs args)
    {
        if (sender is Border border && border.Tag is Note note)
        {
            args.Data.Properties.Add("NoteFilePath", note.FilePath);
            args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }
    }

    public Dictionary<string, bool> GetFolderExpandedStates()
    {
        return _rootFolder is not null ? _folderTreeService.GetExpandedStates(_rootFolder) : [];
    }

    public void RestoreFolderExpandedStates(Dictionary<string, bool> states)
    {
        if (_rootFolder is not null)
        {
            _folderTreeService.RestoreExpandedStates(_rootFolder, states);
        }
    }
}
