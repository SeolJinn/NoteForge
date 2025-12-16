using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using NoteForge.Models;

namespace NoteForge.Controls;

public sealed partial class VaultSelector : UserControl
{
    public event EventHandler<VaultInfo>? VaultSelected;
    public event EventHandler? ManageVaultsRequested;

    public VaultSelector()
    {
        InitializeComponent();
    }

    public void SetVaultName(string name)
    {
        CurrentVaultName.Text = name;
    }

    private void OnVaultSelectorClicked(object sender, RoutedEventArgs e)
    {
    }

    private void OnVaultFlyoutOpening(object sender, object e)
    {
        VaultDropdownIcon.Glyph = "\uE70E";
        var vaults = App.NoteService.GetRecentVaults();
        VaultsList.ItemsSource = vaults;
    }

    private void OnVaultFlyoutClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        VaultDropdownIcon.Glyph = "\uE70D";
    }

    private void OnVaultSelected(object sender, SelectionChangedEventArgs e)
    {
        VaultsList.SelectedItem = null;
    }

    private void OnVaultItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is VaultInfo vaultInfo)
        {
            VaultFlyout.Hide();
            VaultSelected?.Invoke(this, vaultInfo);
        }
    }

    private void OnManageVaultsClicked(object sender, RoutedEventArgs e)
    {
        VaultFlyout.Hide();
        ManageVaultsRequested?.Invoke(this, EventArgs.Empty);
    }
}