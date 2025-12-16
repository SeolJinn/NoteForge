using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace NoteForge.Helpers;

public sealed class AsyncDebounceHelper : IDisposable
{
    private readonly DispatcherQueue _dispatcher;
    private readonly DispatcherQueueTimer _timer;
    private Func<Task>? _pendingAction;
    private bool _isExecuting;

    public AsyncDebounceHelper(DispatcherQueue dispatcher, TimeSpan interval)
    {
        _dispatcher = dispatcher;
        _timer = _dispatcher.CreateTimer();
        _timer.Interval = interval;
        _timer.IsRepeating = false;
        _timer.Tick += OnTimerTick;
    }

    public void Debounce(Func<Task> action)
    {
        _timer.Stop();
        _pendingAction = action;
        _timer.Start();
    }

    public void Cancel()
    {
        _timer.Stop();
        _pendingAction = null;
    }

    public async Task FlushAsync()
    {
        _timer.Stop();
        var action = _pendingAction;
        _pendingAction = null;
        if (action is not null && !_isExecuting)
        {
            _isExecuting = true;
            try
            {
                await action();
            }
            finally
            {
                _isExecuting = false;
            }
        }
    }

    private async void OnTimerTick(DispatcherQueueTimer sender, object args)
    {
        if (_isExecuting) return;

        var action = _pendingAction;
        _pendingAction = null;

        if (action is not null)
        {
            _isExecuting = true;
            try
            {
                await action();
            }
            finally
            {
                _isExecuting = false;
            }
        }
    }

    public void Dispose()
    {
        _timer.Stop();
        _pendingAction = null;
    }
}
