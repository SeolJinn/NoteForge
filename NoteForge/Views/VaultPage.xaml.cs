using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NoteForge.Models;
using System.IO;
using System.Linq;

namespace NoteForge.Views;

public sealed partial class VaultPage : VaultPageBase
{
    public VaultPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        LoadRecentVaults();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        
        if (_isInVaultManager && e.Content is CreateVaultPage createPage)
        {
            createPage.VaultOpened += (s, path) => RaiseVaultOpened(path);
        }
    }

    private void LoadRecentVaults()
    {
        var recentVaults = _noteService.GetRecentVaults();

        if (recentVaults.Count > 0)
        {
            RecentVaultsCollection.ItemsSource = recentVaults;
            RecentVaultsCollection.Visibility = Visibility.Visible;
            EmptyStatePanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            RecentVaultsCollection.Visibility = Visibility.Collapsed;
            EmptyStatePanel.Visibility = Visibility.Visible;
        }
    }

    private void OnCreateVaultClicked(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(CreateVaultPage), _isInVaultManager);
    }

    private async void OnOpenVaultClicked(object sender, RoutedEventArgs e)
    {
        var path = await _dialogService.PickFolderAsync();
        if (!string.IsNullOrEmpty(path))
        {
            _noteService.SetVaultPath(path);
            HandleVaultOpened(path);
        }
    }

    private async void OnRecentVaultSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is VaultInfo selectedVault)
        {
            if (Directory.Exists(selectedVault.Path))
            {
                _noteService.SetVaultPath(selectedVault.Path);
                HandleVaultOpened(selectedVault.Path);
            }
            else
            {
                await ShowVaultNotFoundErrorAsync();
                LoadRecentVaults();
            }

            RecentVaultsCollection.SelectedItem = null;
        }
    }
}