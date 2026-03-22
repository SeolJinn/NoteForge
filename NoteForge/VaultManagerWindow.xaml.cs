using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using NoteForge.Views;

namespace NoteForge;

public sealed partial class VaultManagerWindow : Window
{
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

    public event EventHandler<string>? VaultSelected;

    public VaultManagerWindow()
    {
        InitializeComponent();
        
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.Resize(new Windows.Graphics.SizeInt32(500, 600));
        appWindow.SetIcon("Assets/app.ico");

        SetRoundedCorners(hwnd);
        
        RootFrame.Navigated += OnFrameNavigated;
        
        RootFrame.Navigate(typeof(VaultPage), true);
    }

    private static void SetRoundedCorners(IntPtr hwnd)
    {
        try
        {
            int preference = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }
        catch
        {
        }
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        if (e.Content is VaultPage page)
        {
            page.VaultOpened += OnVaultOpened;
        }
    }

    private void OnVaultOpened(object? sender, string path)
    {
        VaultSelected?.Invoke(this, path);
        Close();
    }
}

