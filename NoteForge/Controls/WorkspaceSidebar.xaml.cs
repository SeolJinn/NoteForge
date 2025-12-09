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
    public event EventHandler<string>? CreateSectionRequested;
    public event EventHandler<NoteSection>? RenameSectionRequested;
    public event EventHandler<NoteSection>? DeleteSectionRequested;
    public event EventHandler<(Note Note, string TargetSectionId)>? NoteMovedToSection;
    public event EventHandler? SectionsReordered;

    private readonly List<SectionView> _sectionViews = [];
    private List<Note> _allNotes = [];
    private SidebarViewMode _currentMode = SidebarViewMode.Folder;

    public WorkspaceSidebar()
    {
        InitializeComponent();
    }

    public void LoadSections(IEnumerable<NoteSection> sections)
    {
        SectionsListView.Items.Clear();
        _sectionViews.Clear();

        foreach (var section in sections)
        {
            var sectionView = new SectionView(section);
            sectionView.NoteSelected += OnSectionNoteSelected;
            sectionView.ToggleFavoriteRequested += OnSectionToggleFavorite;
            sectionView.RenameSectionRequested += (s, e) => RenameSectionRequested?.Invoke(this, section);
            sectionView.DeleteSectionRequested += (s, e) => DeleteSectionRequested?.Invoke(this, section);
            sectionView.NoteMovedToSection += OnNoteMovedToSection;

            _sectionViews.Add(sectionView);
            SectionsListView.Items.Add(sectionView);
        }
    }

    private void OnSectionDragCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        var reorderedSections = new List<NoteSection>();
        foreach (var item in SectionsListView.Items)
        {
            if (item is SectionView sectionView)
            {
                reorderedSections.Add(sectionView.Section);
            }
        }

        App.SectionService.Sections.Clear();
        foreach (var section in reorderedSections)
        {
            App.SectionService.Sections.Add(section);
        }

        SectionsReordered?.Invoke(this, EventArgs.Empty);
    }

    private void OnSectionNoteSelected(object? sender, Note note)
    {
        NoteSelected?.Invoke(this, note);
    }

    private void OnSectionToggleFavorite(object? sender, Note note)
    {
        ToggleFavoriteRequested?.Invoke(this, note);
    }

    private void OnNoteMovedToSection(object? sender, (Note Note, string TargetSectionId) e)
    {
        NoteMovedToSection?.Invoke(this, e);
    }

    private void OnCreateSectionClicked(object sender, RoutedEventArgs e)
    {
        CreateSectionRequested?.Invoke(this, string.Empty);
    }

    public void SetVaultName(string name)
    {
        CurrentVaultName.Text = name;
    }

    public void SetSelectedNote(Note? note)
    {
        foreach (var sectionView in _sectionViews)
        {
            sectionView.SetSelectedNote(note);
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

        if (mode == SidebarViewMode.Folder)
        {
            SectionsListView.Visibility = Visibility.Visible;
            SearchView.Visibility = Visibility.Collapsed;
        }
        else
        {
            SectionsListView.Visibility = Visibility.Collapsed;
            SearchView.Visibility = Visibility.Visible;
            SearchTextBox.Text = string.Empty;
            ShowSearchOptions();
        }
    }

    public void LoadNotesForSearch(IEnumerable<Note> notes)
    {
        _allNotes = notes.ToList();
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

        if (results.Count == 0)
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
                    ? remainingQuery.Substring(start, end - start)
                    : remainingQuery.Substring(start);

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
                    contentMatches[contentMatches.Count - 1].IsLast = true;
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
            if (note != null)
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
}