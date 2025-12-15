using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using NoteForge.Models;

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
    public event EventHandler<Note>? ToggleFavoriteRequested;
    public event EventHandler<Folder>? CreateFolderRequested;
    public event EventHandler<Folder>? RenameFolderRequested;
    public event EventHandler<Folder>? DeleteFolderRequested;
    public event EventHandler<(Note Note, Folder TargetFolder)>? NoteMovedToFolder;

    private readonly List<FolderView> _folderViews = [];
    private SectionView? _favoritesView;
    private List<Note> _allNotes = [];
    private SidebarViewMode _currentMode = SidebarViewMode.Folder;
    private Folder? _rootFolder;

    public WorkspaceSidebar()
    {
        InitializeComponent();
    }

    public void LoadFolders(Folder rootFolder, NoteSection? favoritesSection)
    {
        FoldersContainer.Children.Clear();
        _folderViews.Clear();
        _favoritesView = null;
        _rootFolder = rootFolder;

        if (favoritesSection is not null && favoritesSection.IsVisible)
        {
            _favoritesView = new SectionView(favoritesSection);
            _favoritesView.NoteSelected += OnFolderNoteSelected;
            _favoritesView.ToggleFavoriteRequested += OnFolderToggleFavorite;
            FoldersContainer.Children.Add(_favoritesView);
        }

        foreach (var subfolder in rootFolder.SubFolders)
        {
            var folderView = CreateFolderView(subfolder);
            _folderViews.Add(folderView);
            FoldersContainer.Children.Add(folderView);
        }

        foreach (var note in rootFolder.Notes)
        {
            var noteItem = CreateNoteItem(note);
            FoldersContainer.Children.Add(noteItem);
        }
    }

    private Border CreateNoteItem(Note note)
    {
        var border = new Border
        {
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(8),
            Margin = new Thickness(10, 2, 10, 2),
            Padding = new Thickness(15, 10, 15, 10),
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                note.IsSelected
                    ? Windows.UI.Color.FromArgb(255, 52, 52, 52)
                    : Microsoft.UI.Colors.Transparent),
            Tag = note,
            CanDrag = true
        };

        var textBlock = new TextBlock
        {
            Text = note.Filename,
            FontSize = 14,
            Foreground = (Microsoft.UI.Xaml.Media.SolidColorBrush)Application.Current.Resources["TextPrimary"],
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        border.Child = textBlock;
        border.Tapped += (s, e) => OnFolderNoteSelected(s, note);
        border.DragStarting += OnRootNoteDragStarting;

        var menuFlyout = new MenuFlyout
        {
            Placement = FlyoutPlacementMode.RightEdgeAlignedTop
        };

        MenuFlyoutItem menuItem = new()
        {
            Text = "Add to favorites",
            Tag = note,
            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 218, 218, 218)),
            Icon = new FontIcon { Glyph = "\uE734", FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets") }
        };
        menuItem.Click += (s, e) => OnFolderToggleFavorite(s, note);

        menuFlyout.Items.Add(menuItem);
        border.ContextFlyout = menuFlyout;

        return border;
    }

    private FolderView CreateFolderView(Folder folder)
    {
        var folderView = new FolderView(folder, _rootFolder);
        folderView.NoteSelected += OnFolderNoteSelected;
        folderView.CreateSubfolderRequested += OnCreateSubfolderRequested;
        folderView.RenameFolderRequested += OnRenameFolderRequested;
        folderView.DeleteFolderRequested += OnDeleteFolderRequested;
        folderView.NoteMovedToFolder += OnNoteMovedToFolder;
        folderView.ToggleFavoriteRequested += OnFolderToggleFavorite;
        return folderView;
    }

    private void OnFolderNoteSelected(object? sender, Note note)
    {
        NoteSelected?.Invoke(this, note);
    }

    private void OnFolderToggleFavorite(object? sender, Note note)
    {
        ToggleFavoriteRequested?.Invoke(this, note);
    }

    private void OnCreateSubfolderRequested(object? sender, Folder folder)
    {
        CreateFolderRequested?.Invoke(this, folder);
    }

    private void OnRenameFolderRequested(object? sender, Folder folder)
    {
        RenameFolderRequested?.Invoke(this, folder);
    }

    private void OnDeleteFolderRequested(object? sender, Folder folder)
    {
        DeleteFolderRequested?.Invoke(this, folder);
    }

    private void OnNoteMovedToFolder(object? sender, (Note Note, Folder TargetFolder) e)
    {
        NoteMovedToFolder?.Invoke(this, e);
    }

    private void OnCreateFolderClicked(object sender, RoutedEventArgs e)
    {
        CreateFolderRequested?.Invoke(this, null!);
    }

    public void SetVaultName(string name)
    {
        CurrentVaultName.Text = name;
    }

    public void SetSelectedNote(Note? note)
    {
        _favoritesView?.SetSelectedNote(note);

        foreach (var folderView in _folderViews)
        {
            folderView.SetSelectedNote(note);
        }

        foreach (UIElement child in FoldersContainer.Children)
        {
            if (child is Border border && border.Tag is Note borderNote)
            {
                var isSelected = note is not null &&
                                !string.IsNullOrEmpty(borderNote.FilePath) &&
                                !string.IsNullOrEmpty(note.FilePath) &&
                                string.Equals(borderNote.FilePath, note.FilePath, StringComparison.OrdinalIgnoreCase);

                borderNote.IsSelected = isSelected;

                border.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    isSelected
                        ? Windows.UI.Color.FromArgb(255, 52, 52, 52)
                        : Microsoft.UI.Colors.Transparent);
            }
        }
    }

    public void SetVaultsSource(object itemsSource)
    {
        VaultsList.ItemsSource = itemsSource;
    }

    private void OnVaultSelectorClicked(object sender, RoutedEventArgs e)
    {
    }

    private void OnVaultFlyoutOpening(object sender, object e)
    {
        VaultDropdownIcon.Glyph = "\uE70E";
        var vaults = App.NoteService.GetRecentVaults();
        VaultsList.ItemsSource = vaults;
    }

    private void OnVaultFlyoutClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        VaultDropdownIcon.Glyph = "\uE70D";
    }

    private void OnVaultSelected(object sender, SelectionChangedEventArgs e)
    {
        VaultsList.SelectedItem = null;
    }

    private void OnVaultItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is VaultInfo vaultInfo)
        {
            VaultFlyout.Hide();
            VaultSelected?.Invoke(this, vaultInfo);
        }
    }

    private void OnManageVaultsClicked(object sender, RoutedEventArgs e)
    {
        VaultFlyout.Hide();
        ManageVaultsRequested?.Invoke(this, EventArgs.Empty);
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
            SearchTextBox.Text = string.Empty;
            ShowSearchOptions();
        }
    }

    public void LoadNotesForSearch(IEnumerable<Note> notes)
    {
        _allNotes = [.. notes];
    }

    private void OnSearchTextBoxLoaded(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Focus(FocusState.Programmatic);
    }

    private void OnSearchTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        var query = SearchTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            ShowSearchOptions();
        }
    }

    private void OnPathFilterClicked(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = "path:";
        SearchTextBox.Focus(FocusState.Programmatic);
        SearchTextBox.SelectionStart = SearchTextBox.Text.Length;
    }

    private void OnFileFilterClicked(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = "file:";
        SearchTextBox.Focus(FocusState.Programmatic);
        SearchTextBox.SelectionStart = SearchTextBox.Text.Length;
    }

    private void ShowSearchOptions()
    {
        SearchOptionsMenu.Visibility = Visibility.Visible;
        ResultsContainer.Visibility = Visibility.Collapsed;
    }

    private void ShowResults()
    {
        SearchOptionsMenu.Visibility = Visibility.Collapsed;
        ResultsContainer.Visibility = Visibility.Visible;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateSearchResults();
    }

    private void UpdateSearchResults()
    {
        var query = SearchTextBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            ShowSearchOptions();
            return;
        }

        ShowResults();

        var results = ParseAndSearch(query);
        ResultsListView.ItemsSource = results;

        if (results.Count is 0)
        {
            NoResultsText.Visibility = Visibility.Visible;
            ResultsListView.Visibility = Visibility.Collapsed;
        }
        else
        {
            NoResultsText.Visibility = Visibility.Collapsed;
            ResultsListView.Visibility = Visibility.Visible;
        }
    }

    private List<SearchResult> ParseAndSearch(string query)
    {
        var filters = new Dictionary<string, string>();
        var remainingQuery = query;

        var filterPrefixes = new[] { "file:", "path:" };

        foreach (var prefix in filterPrefixes)
        {
            var index = remainingQuery.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var start = index + prefix.Length;
                var end = remainingQuery.IndexOf(' ', start);
                var value = end >= 0
                    ? remainingQuery[start..end]
                    : remainingQuery[start..];

                filters[prefix.TrimEnd(':')] = value.Trim();
                remainingQuery = remainingQuery.Remove(index, (end >= 0 ? end : remainingQuery.Length) - index);
            }
        }

        remainingQuery = remainingQuery.Trim();

        var searchResults = new List<SearchResult>();

        foreach (var note in _allNotes)
        {
            var matchesFile = !filters.TryGetValue("file", out var fileFilter) ||
                             note.Filename.Contains(fileFilter, StringComparison.OrdinalIgnoreCase);

            var matchesPath = !filters.TryGetValue("path", out var pathFilter) ||
                             note.FilePath.Contains(pathFilter, StringComparison.OrdinalIgnoreCase);

            if (!matchesFile || !matchesPath)
                continue;

            if (string.IsNullOrWhiteSpace(remainingQuery))
            {
                searchResults.Add(new SearchResult(note, remainingQuery) { MatchesInTitle = true });
                continue;
            }

            var titleMatches = note.Filename.Contains(remainingQuery, StringComparison.OrdinalIgnoreCase);
            var contentMatches = new List<MatchingLine>();

            if (!string.IsNullOrEmpty(note.Text))
            {
                var lines = note.Text.Split(['\r', '\n'], StringSplitOptions.None);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.IsNullOrEmpty(lines[i]))
                        continue;

                    if (lines[i].Contains(remainingQuery, StringComparison.OrdinalIgnoreCase))
                    {
                        var trimmedLine = lines[i].Trim();
                        if (!string.IsNullOrEmpty(trimmedLine))
                        {
                            contentMatches.Add(new MatchingLine(trimmedLine, remainingQuery, i + 1));
                        }
                    }
                }
            }

            if (titleMatches || contentMatches.Count > 0)
            {
                if (contentMatches.Count > 0)
                {
                    contentMatches[^1].IsLast = true;
                }

                searchResults.Add(new SearchResult(note, remainingQuery)
                {
                    MatchesInTitle = titleMatches,
                    MatchingLines = contentMatches
                });
            }
        }

        return searchResults;
    }

    private void OnResultSelected(object sender, SelectionChangedEventArgs e)
    {
        ResultsListView.SelectedItem = null;
    }

    private void OnSearchResultHeaderTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is Grid grid && grid.Tag is SearchResult result)
        {
            if (result.MatchingLines.Count > 0)
            {
                result.IsExpanded = !result.IsExpanded;
            }
            else
            {
                NoteSelected?.Invoke(this, result.Note);
            }
        }
    }

    private void OnMatchingLineClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is MatchingLine matchingLine)
        {
            var note = FindNoteForMatchingLine(matchingLine);
            if (note is not null)
            {
                MatchingLineSelected?.Invoke(this, (note, matchingLine.LineNumber));
            }
        }
    }

    private Note? FindNoteForMatchingLine(MatchingLine matchingLine)
    {
        if (ResultsListView.ItemsSource is IEnumerable<SearchResult> results)
        {
            foreach (var result in results)
            {
                if (result.MatchingLines.Contains(matchingLine))
                {
                    return result.Note;
                }
            }
        }
        return null;
    }

    private void OnRootDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey("NoteFilePath"))
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            e.DragUIOverride.Caption = "Move to vault root";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
        }
        else
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
        }
    }

    private void OnRootDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("NoteFilePath", out var noteFilePathObj) && noteFilePathObj is string noteFilePath)
        {
            if (_rootFolder is null)
                return;

            Note? note = FindNoteInFolderTree(_rootFolder, noteFilePath)
                         ?? _allNotes.FirstOrDefault(n => n.FilePath == noteFilePath);

            if (note is not null)
            {
                NoteMovedToFolder?.Invoke(this, (Note: note, TargetFolder: _rootFolder));
            }
        }
    }

    private Note? FindNoteInFolderTree(Folder folder, string noteFilePath)
    {
        var note = folder.Notes.FirstOrDefault(n => n.FilePath == noteFilePath);
        if (note is not null)
        {
            return note;
        }

        foreach (var subfolder in folder.SubFolders)
        {
            note = FindNoteInFolderTree(subfolder, noteFilePath);
            if (note is not null)
            {
                return note;
            }
        }

        return null;
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
        var states = new Dictionary<string, bool>();
        if (_rootFolder is not null)
        {
            CollectExpandedStates(_rootFolder, states);
        }
        return states;
    }

    private void CollectExpandedStates(Folder folder, Dictionary<string, bool> states)
    {
        states[folder.DirectoryPath] = folder.IsExpanded;
        foreach (var subfolder in folder.SubFolders)
        {
            CollectExpandedStates(subfolder, states);
        }
    }

    public void RestoreFolderExpandedStates(Dictionary<string, bool> states)
    {
        if (_rootFolder is not null)
        {
            ApplyExpandedStates(_rootFolder, states);
        }
    }

    private void ApplyExpandedStates(Folder folder, Dictionary<string, bool> states)
    {
        if (states.TryGetValue(folder.DirectoryPath, out var isExpanded))
        {
            folder.IsExpanded = isExpanded;
        }
        foreach (var subfolder in folder.SubFolders)
        {
            ApplyExpandedStates(subfolder, states);
        }
    }
}
