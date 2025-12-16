using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Models;

namespace NoteForge.Controls;

public sealed partial class SearchResultsPanel : UserControl
{
    public event EventHandler<Note>? NoteSelected;
    public event EventHandler<(Note Note, int LineNumber)>? MatchingLineSelected;

    public SearchResultsPanel()
    {
        InitializeComponent();
    }

    public void SetResults(List<SearchResult> results)
    {
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
}