using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NoteForge.Models;
using NoteForge.Services;
using System;
using System.IO;
using System.Linq;

namespace NoteForge.Views;

public sealed partial class VaultPage : Page
{
    private readonly INoteService _noteService;

    public VaultPage()
    {
        InitializeComponent();
        _noteService = App.NoteService;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        LoadRecentVaults();
    }

    private void LoadRecentVaults()
    {
        var recentVaults = _noteService.GetRecentVaults();

        if (recentVaults.Count > 0)
        {
            RecentVaultsCollection.ItemsSource = recentVaults;
            RecentVaultsCollection.Visibility = Visibility.Visible;
            EmptyStateLabel.Visibility = Visibility.Collapsed;
        }
        else
        {
            RecentVaultsCollection.Visibility = Visibility.Collapsed;
            EmptyStateLabel.Visibility = Visibility.Visible;
        }
    }

    private void OnCreateVaultClicked(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(CreateVaultPage));
    }

    private async void OnOpenVaultClicked(object sender, RoutedEventArgs e)
    {
        var path = await _noteService.PickFolderAsync();
        if (!string.IsNullOrEmpty(path))
        {
            OpenMainApp();
        }
    }

    private async void OnRecentVaultSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is VaultInfo selectedVault)
        {
            if (Directory.Exists(selectedVault.Path))
            {
                _noteService.SetVaultPath(selectedVault.Path);
                OpenMainApp();
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = "Vault folder no longer exists.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                LoadRecentVaults();
            }

            RecentVaultsCollection.SelectedItem = null;
        }
    }

    private void OpenMainApp()
    {
        Frame.Navigate(typeof(WorkspacePage));
    }
}

