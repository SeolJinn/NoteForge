using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Models;
using NoteForge.Services.Search;

namespace NoteForge.Controls;

public sealed partial class SearchPanel : UserControl
{
    public event EventHandler<Note>? NoteSelected;

    private List<Note> _allNotes = [];
    private readonly ISearchStrategy<IEnumerable<Note>, Note> _searchStrategy;

    public SearchPanel()
    {
        InitializeComponent();
        _searchStrategy = new NoteFileSearchStrategy();
    }

    public void LoadNotes(IEnumerable<Note> notes)
    {
        _allNotes = notes.ToList();
        SearchTextBox.Text = string.Empty;
        ShowSearchOptions();
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

    private void OnSearchTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
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
        UpdateResults();
    }

    private void UpdateResults()
    {
        var query = SearchTextBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            ShowSearchOptions();
            return;
        }

        ShowResults();

        var results = _searchStrategy.Search(_allNotes, query).ToList();
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

    private void OnResultSelected(object sender, SelectionChangedEventArgs e)
    {
        ResultsListView.SelectedItem = null;
    }

    private void OnNoteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Note note)
        {
            NoteSelected?.Invoke(this, note);
        }
    }
}
