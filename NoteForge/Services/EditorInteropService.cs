using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace NoteForge.Services;

public sealed class EditorInteropService(ILogger<EditorInteropService> logger) : IDisposable
{
    private readonly ILogger<EditorInteropService> _logger = logger;
    private WebView2? _webView;
    private TaskCompletionSource? _readyTcs;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();
    private readonly ConcurrentQueue<Func<Task>> _pendingCommands = new();
    private int _requestCounter;
    private bool _disposed;

    public bool IsReady { get; private set; }

    public event EventHandler? ContentChanged;
    public event EventHandler<string>? LinkClicked;
    public event EventHandler? SaveRequested;
    public event EventHandler? EditorReady;

    public async Task InitializeAsync(WebView2 webView)
    {
        _webView = webView;
        _readyTcs = new TaskCompletionSource();

        await _webView.EnsureCoreWebView2Async();
        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("NoteForge.Templates.InlineEditor.html") ?? throw new InvalidOperationException("InlineEditor.html embedded resource not found");
        using var reader = new System.IO.StreamReader(stream);
        var html = await reader.ReadToEndAsync();
        _webView.NavigateToString(html);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        cts.Token.Register(() => _readyTcs.TrySetCanceled());
        await _readyTcs.Task;

        IsReady = true;

        while (_pendingCommands.TryDequeue(out var command))
        {
            await command();
        }

        EditorReady?.Invoke(this, EventArgs.Empty);
    }

    public Task SetContentAsync(string text)
    {
        return EnqueueOrExecuteAsync(() =>
            PostMessageAsync(new { type = "setContent", text }));
    }

    public async Task<string> GetContentAsync(TimeSpan? timeout = null)
    {
        if (!IsReady)
            throw new InvalidOperationException("Cannot get content before editor is ready");

        var id = $"req-{Interlocked.Increment(ref _requestCounter)}";
        var tcs = new TaskCompletionSource<string>();
        _pendingRequests[id] = tcs;

        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(5);
        using var cts = new CancellationTokenSource(effectiveTimeout);
        cts.Token.Register(() =>
        {
            if (_pendingRequests.TryRemove(id, out var removed))
                removed.TrySetException(new TimeoutException($"GetContentAsync timed out after {effectiveTimeout.TotalSeconds}s"));
        });

        await PostMessageAsync(new { type = "getContent", id });
        return await tcs.Task;
    }

    public Task FocusAsync()
    {
        return EnqueueOrExecuteAsync(() =>
            PostMessageAsync(new { type = "focus" }));
    }

    public Task SetReadOnlyAsync(bool value)
    {
        return EnqueueOrExecuteAsync(() =>
            PostMessageAsync(new { type = "setReadOnly", value }));
    }

    public Task NavigateToLineAsync(int line)
    {
        return EnqueueOrExecuteAsync(() =>
            PostMessageAsync(new { type = "navigateToLine", line }));
    }

    private Task EnqueueOrExecuteAsync(Func<Task> action)
    {
        if (IsReady) return action();
        _pendingCommands.Enqueue(action);
        return Task.CompletedTask;
    }

    private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            var json = args.TryGetWebMessageAsString() ?? args.WebMessageAsJson;
            _logger.LogDebug("WebView2 message received: {Json}", json);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            var msgType = root.GetProperty("type").GetString();

            switch (msgType)
            {
                case "ready":
                    _readyTcs?.TrySetResult();
                    break;

                case "contentChanged":
                    ContentChanged?.Invoke(this, EventArgs.Empty);
                    break;

                case "linkClicked":
                    var href = root.GetProperty("href").GetString() ?? "";
                    LinkClicked?.Invoke(this, href);
                    break;

                case "contentResponse":
                    var id = root.GetProperty("id").GetString() ?? "";
                    var text = root.GetProperty("text").GetString() ?? "";
                    if (_pendingRequests.TryRemove(id, out var tcs))
                        tcs.TrySetResult(text);
                    break;

                case "saveRequested":
                    SaveRequested?.Invoke(this, EventArgs.Empty);
                    break;

                case "error":
                    var message = root.GetProperty("message").GetString() ?? "unknown";
                    var source = root.GetProperty("source").GetString() ?? "unknown";
                    _logger.LogWarning("Editor JS error from {Source}: {Message}", source, message);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process WebView2 message");
        }
    }

    private Task PostMessageAsync(object message)
    {
        if (_webView?.CoreWebView2 is null) return Task.CompletedTask;
        var json = System.Text.Json.JsonSerializer.Serialize(message);
        _webView.CoreWebView2.PostWebMessageAsJson(json);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_webView?.CoreWebView2 is not null)
            _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;

        foreach (var pending in _pendingRequests.Values)
            pending.TrySetCanceled();

        _pendingRequests.Clear();
        _pendingCommands.Clear();
        _readyTcs?.TrySetCanceled();

        try
        {
            _webView?.Close();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "WebView2 close failed during dispose");
        }
    }
}
