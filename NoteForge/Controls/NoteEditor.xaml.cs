using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using NoteForge.Services.Search;

namespace NoteForge.Controls;

public sealed partial class NoteEditor : UserControl
{
    public event EventHandler<string>? TitleChanged;
    public event EventHandler? TitleUnfocused;
    public event EventHandler<string>? ContentChanged;
    public event EventHandler? GenerateSummaryRequested;
    public event EventHandler? CloseSummaryRequested;
    public event EventHandler? TogglePreviewRequested;

    private bool _suppressEvents;
    private readonly ISearchStrategy<string, TextMatch> _textSearchStrategy;
    private List<TextMatch> _searchMatches = [];
    private int _currentMatchIndex = -1;

    public NoteEditor()
    {
        InitializeComponent();
        _textSearchStrategy = new InFileTextSearchStrategy();
    }

    public void SetTitle(string title)
    {
        _suppressEvents = true;
        NoteTitleEntry.Text = title;
        _suppressEvents = false;
    }

    public void SetContent(string content)
    {
        _suppressEvents = true;
        NoteContentEditor.Text = content;
        _suppressEvents = false;
    }

    public void ShowAiSummary(string summary)
    {
        AiSummaryContainer.Visibility = Visibility.Visible;
        AiSummaryText.Text = summary;
    }

    public void HideAiSummary()
    {
        AiSummaryContainer.Visibility = Visibility.Collapsed;
    }

    public void SetAiSummaryText(string text)
    {
        AiSummaryText.Text = text;
    }

    public void AppendAiSummaryText(string text)
    {
        AiSummaryText.Text += text;
    }

    public void SetSummaryButtonEnabled(bool enabled)
    {
        GenerateSummaryButton.IsEnabled = enabled;
    }

    public WebView2 GetPreviewWebView()
    {
        return PreviewWebView;
    }

    public void SetPreviewColumnWidth(double width)
    {
        PreviewColumn.Width = new GridLength(width, width == 0 ? GridUnitType.Pixel : GridUnitType.Star);
        PreviewToggleBtn.Content = width is 0 ? "<" : ">";
    }

    public double GetPreviewColumnWidth()
    {
        return PreviewColumn.Width.Value;
    }

    public void NavigateToLine(int lineNumber)
    {
        var text = NoteContentEditor.Text;
        if (string.IsNullOrEmpty(text) || lineNumber < 1)
            return;

        var lines = text.Split(['\r', '\n'], StringSplitOptions.None);
        if (lineNumber > lines.Length)
            return;

        var position = 0;
        for (int i = 0; i < lineNumber - 1 && i < lines.Length; i++)
        {
            position += lines[i].Length + 1;
        }

        NoteContentEditor.Focus(FocusState.Programmatic);
        NoteContentEditor.SelectionStart = position;
        NoteContentEditor.SelectionLength = lines[lineNumber - 1].Length;
    }

    private void OnTitleTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_suppressEvents)
        {
            TitleChanged?.Invoke(this, NoteTitleEntry.Text);
        }
    }

    private void OnTitleUnfocused(object sender, RoutedEventArgs e)
    {
        TitleUnfocused?.Invoke(this, EventArgs.Empty);
    }

    private void OnContentTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_suppressEvents)
        {
            ContentChanged?.Invoke(this, NoteContentEditor.Text);
        }
    }

    private void OnGenerateSummaryClicked(object sender, RoutedEventArgs e)
    {
        GenerateSummaryRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCloseSummaryClicked(object sender, RoutedEventArgs e)
    {
        CloseSummaryRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnTogglePreviewClicked(object sender, RoutedEventArgs e)
    {
        TogglePreviewRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnEditorKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.F &&
            (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & Windows.UI.Core.CoreVirtualKeyStates.Down) is not 0)
        {
            e.Handled = true;
            ShowSearchBar();
        }
    }

    private void OnSearchKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Escape)
        {
            e.Handled = true;
            HideSearchBar();
        }
        else if (e.Key is VirtualKey.Enter)
        {
            e.Handled = true;
            var isShiftPressed = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) & Windows.UI.Core.CoreVirtualKeyStates.Down) is not 0;
            if (isShiftPressed)
            {
                NavigateToPreviousMatch();
            }
            else
            {
                NavigateToNextMatch();
            }
        }
        else if (e.Key is VirtualKey.Down)
        {
            e.Handled = true;
            NavigateToNextMatch();
        }
        else if (e.Key is VirtualKey.Up)
        {
            e.Handled = true;
            NavigateToPreviousMatch();
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        PerformSearch();
    }

    private void OnPreviousMatchClicked(object sender, RoutedEventArgs e)
    {
        NavigateToPreviousMatch();
    }

    private void OnNextMatchClicked(object sender, RoutedEventArgs e)
    {
        NavigateToNextMatch();
    }

    private void OnCloseSearchClicked(object sender, RoutedEventArgs e)
    {
        HideSearchBar();
    }

    private void ShowSearchBar()
    {
        SearchBar.Visibility = Visibility.Visible;
        SearchBox.Focus(FocusState.Programmatic);
    }

    private void HideSearchBar()
    {
        SearchBar.Visibility = Visibility.Collapsed;
        _searchMatches.Clear();
        _currentMatchIndex = -1;
        NoteContentEditor.Focus(FocusState.Programmatic);
    }

    private void PerformSearch()
    {
        _searchMatches.Clear();
        _currentMatchIndex = -1;

        var searchText = SearchBox.Text;
        if (string.IsNullOrEmpty(searchText))
        {
            return;
        }

        var content = NoteContentEditor.Text;
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        _searchMatches = [.. _textSearchStrategy.Search(content, searchText)];

        if (_searchMatches.Count is not 0)
        {
            _currentMatchIndex = 0;
        }
    }

    private void NavigateToNextMatch()
    {
        if (_searchMatches.Count is 0)
        {
            return;
        }

        _currentMatchIndex = (_currentMatchIndex + 1) % _searchMatches.Count;
        HighlightCurrentMatch();
    }

    private void NavigateToPreviousMatch()
    {
        if (_searchMatches.Count is 0)
        {
            return;
        }

        _currentMatchIndex--;
        if (_currentMatchIndex < 0)
        {
            _currentMatchIndex = _searchMatches.Count - 1;
        }
        HighlightCurrentMatch();
    }

    private void HighlightCurrentMatch()
    {
        if (_currentMatchIndex < 0 || _currentMatchIndex >= _searchMatches.Count)
        {
            return;
        }

        var match = _searchMatches[_currentMatchIndex];

        NoteContentEditor.SelectionStart = match.Position;
        NoteContentEditor.SelectionLength = match.Length;
        NoteContentEditor.Focus(FocusState.Programmatic);
    }
}
