using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NoteForge.Controls;

public sealed partial class ToastNotification : UserControl
{
    private DispatcherQueueTimer? _autoDismissTimer;

    public ToastNotification()
    {
        InitializeComponent();
        Visibility = Visibility.Collapsed;
    }

    public void Show(string message, int autoDismissMs = 4000)
    {
        _autoDismissTimer?.Stop();

        MessageText.Text = message;
        Visibility = Visibility.Visible;
        FadeInStoryboard.Begin();

        _autoDismissTimer = DispatcherQueue.CreateTimer();
        _autoDismissTimer.Interval = System.TimeSpan.FromMilliseconds(autoDismissMs);
        _autoDismissTimer.IsRepeating = false;
        _autoDismissTimer.Tick += (_, _) => Dismiss();
        _autoDismissTimer.Start();
    }

    private void Dismiss()
    {
        _autoDismissTimer?.Stop();
        FadeOutStoryboard.Begin();
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e) => Dismiss();

    private void OnFadeOutCompleted(object? sender, object e)
    {
        Visibility = Visibility.Collapsed;
    }
}
