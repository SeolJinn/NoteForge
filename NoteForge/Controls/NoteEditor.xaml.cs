using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    public NoteEditor()
    {
        InitializeComponent();
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
        PreviewToggleBtn.Content = width == 0 ? "<" : ">";
    }

    public double GetPreviewColumnWidth()
    {
        return PreviewColumn.Width.Value;
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
}
