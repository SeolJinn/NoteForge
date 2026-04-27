#if DISABLE_XAML_GENERATED_MAIN
using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace NoteForge;

public static class Program
{
    private const uint WindowsAppSdkMajorMinor = 0x00010008;

    [DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [STAThread]
    private static void Main(string[] args)
    {
        if (!TryInitializeBootstrap())
            return;

        try
        {
            XamlCheckProcessRequirements();
            global::WinRT.ComWrappersSupport.InitializeComWrappers();

            Application.Start(p =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
        finally
        {
            try { Bootstrap.Shutdown(); } catch { }
        }
    }

    private static bool TryInitializeBootstrap()
    {
        try
        {
            Bootstrap.Initialize(WindowsAppSdkMajorMinor);
            return true;
        }
        catch (Exception ex)
        {
            ShowError($"Failed to initialize Windows App SDK runtime:\n\n{ex.Message}\n\nTry reinstalling NoteForge.");
            return false;
        }
    }

    private static void ShowError(string message) =>
        MessageBox(IntPtr.Zero, message, "NoteForge — Fatal Error", 0x10);
}
#endif
