using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Configuration;
using NoteForge.Services;

namespace NoteForge.Controls;

public sealed partial class NoteEditor : UserControl
{
    public event EventHandler<string>? TitleChanged;
    public event EventHandler? TitleUnfocused;
    public event EventHandler? ContentChanged;
    public event EventHandler? GenerateSummaryRequested;
    public event EventHandler? CloseSummaryRequested;
    public event EventHandler<string>? LinkClicked;
    public event EventHandler? SaveRequested;

    private bool _suppressEvents;
    private EditorInteropService? _interopService;
    private bool _initialized;

    public NoteEditor()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
            return;
        _initialized = true;

        GenerateSummaryButton.Visibility = AiSettings.IsAiEnabled ? Visibility.Visible : Visibility.Collapsed;
        AiSettings.ActiveProviderChanged += OnAiEnabledChanged;

        _interopService = App.Services.GetRequiredService<EditorInteropService>();
        _interopService.ContentChanged += OnInteropContentChanged;
        _interopService.LinkClicked += OnInteropLinkClicked;
        _interopService.SaveRequested += OnInteropSaveRequested;

        await _interopService.InitializeAsync(EditorWebView);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        AiSettings.ActiveProviderChanged -= OnAiEnabledChanged;
        if (_interopService is not null)
        {
            _interopService.ContentChanged -= OnInteropContentChanged;
            _interopService.LinkClicked -= OnInteropLinkClicked;
            _interopService.SaveRequested -= OnInteropSaveRequested;
            _interopService.Dispose();
            _interopService = null;
        }
        _initialized = false;
    }

    private void OnAiEnabledChanged()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            GenerateSummaryButton.Visibility = AiSettings.IsAiEnabled ? Visibility.Visible : Visibility.Collapsed;
            if (!AiSettings.IsAiEnabled)
                HideAiSummary();
        });
    }

    private void OnInteropContentChanged(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (!_suppressEvents) ContentChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    private void OnInteropLinkClicked(object? sender, string href)
    {
        DispatcherQueue.TryEnqueue(() => LinkClicked?.Invoke(this, href));
    }

    private void OnInteropSaveRequested(object? sender, EventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => SaveRequested?.Invoke(this, EventArgs.Empty));
    }

    public void SetTitle(string title)
    {
        _suppressEvents = true;
        NoteTitleEntry.Text = title;
        _suppressEvents = false;
    }

    public async Task SetContentAsync(string content)
    {
        if (_interopService is null)
            return;
        await _interopService.SetContentAsync(content);
    }

    public bool IsEditorReady => _interopService?.IsReady is true;

    public async Task<string> GetContentAsync(TimeSpan? timeout = null)
    {
        if (_interopService is null)
            return "";
        return await _interopService.GetContentAsync(timeout);
    }

    public async Task NavigateToLineAsync(int lineNumber)
    {
        if (_interopService is null)
            return;
        await _interopService.NavigateToLineAsync(lineNumber);
    }

    public async Task FocusEditorAsync()
    {
        if (_interopService is null)
            return;
        await _interopService.FocusAsync();
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

    private void OnTitleTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_suppressEvents)
            TitleChanged?.Invoke(this, NoteTitleEntry.Text);
    }

    private void OnTitleUnfocused(object sender, RoutedEventArgs e)
    {
        TitleUnfocused?.Invoke(this, EventArgs.Empty);
    }

    private void OnGenerateSummaryClicked(object sender, RoutedEventArgs e)
    {
        GenerateSummaryRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCloseSummaryClicked(object sender, RoutedEventArgs e)
    {
        CloseSummaryRequested?.Invoke(this, EventArgs.Empty);
    }

}
