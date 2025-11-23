using NoteForge.Models;
using NoteForge.Services;

namespace NoteForge.Views;

public partial class VaultPage : ContentPage
{
    private readonly INoteService _noteService;

    public VaultPage(INoteService noteService)
    {
        InitializeComponent();
        _noteService = noteService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadRecentVaults();
#if WINDOWS
        SetTitleBar();
#endif
    }

    private void LoadRecentVaults()
    {
        var recentVaults = _noteService.GetRecentVaults();
        
        if (recentVaults.Count > 0)
        {
            RecentVaultsCollection.ItemsSource = recentVaults;
            RecentVaultsCollection.IsVisible = true;
            EmptyStateLabel.IsVisible = false;
        }
        else
        {
            RecentVaultsCollection.IsVisible = false;
            EmptyStateLabel.IsVisible = true;
        }
    }

    private async void OnCreateVaultClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new CreateVaultPage(_noteService));
    }

    private async void OnOpenVaultClicked(object sender, EventArgs e)
    {
        var path = await _noteService.PickFolderAsync();
        if (!string.IsNullOrEmpty(path))
        {
            VaultPage.OpenMainApp();
        }
    }

    private void OnRecentVaultSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is VaultInfo selectedVault)
        {
            if (Directory.Exists(selectedVault.Path))
            {
                Application.Current!.Dispatcher.Dispatch(() =>
                {
                    _noteService.SetVaultPath(selectedVault.Path);
                    VaultPage.OpenMainApp();
                });
            }
            else
            {
                DisplayAlertAsync("Error", "Vault folder no longer exists.", "OK");
                LoadRecentVaults(); // Refresh list
            }
            
            // Clear selection
            RecentVaultsCollection.SelectedItem = null;
        }
    }

    private static void OpenMainApp()
    {
        Application.Current!.Windows[0].Page = new AppShell();
    }

#if WINDOWS

    private void OnMinimizeClicked(object sender, EventArgs e)
    {
        var window = this.Window.Handler.PlatformView as Microsoft.UI.Xaml.Window;
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        if (appWindow is not null)
        {
            (appWindow.Presenter as Microsoft.UI.Windowing.OverlappedPresenter)?.Minimize();
        }
    }

    private void SetTitleBar()
    {
        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
            var nativeWindow = handler.PlatformView;
            nativeWindow.Activate();

            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow is not null)
            {
                nativeWindow.ExtendsContentIntoTitleBar = true;
                nativeWindow.SetTitleBar(AppTitleBar.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement);
            }
        });
    }
#endif

    private void OnCloseClicked(object sender, EventArgs e)
    {
        Application.Current?.Quit();
    }
}