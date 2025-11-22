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
}
