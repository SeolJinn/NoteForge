using NoteForge.Services;
using NoteForge.Views;

namespace NoteForge;

public partial class App : Application
{
    public App(IServiceProvider services)
    {
        InitializeComponent();
        Services = services;
    }
    
    public IServiceProvider Services { get; }

#if WINDOWS
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;
#endif

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new NavigationPage(new VaultPage(Services.GetRequiredService<INoteService>())));

        //TODO: Add some sort of resizing mechanism for better UI pages

#if WINDOWS
        // Remove the title bar on Windows completely
        window.Created += (s, e) =>
        {
            if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            {
                var handle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                if (appWindow is not null && appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                {
                    presenter.SetBorderAndTitleBar(false, false);
                    presenter.IsResizable = true;
                    presenter.IsMaximizable = true;
                    presenter.IsMinimizable = true;
                }

                var attributeValue = DWMWCP_ROUND;
                DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref attributeValue, sizeof(int));
            }
        };
#endif

        return window;
    }
}