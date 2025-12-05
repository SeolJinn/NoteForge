using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NoteForge.Interfaces;

namespace NoteForge.Views;

public abstract class VaultPageBase : Page
{
    protected readonly INoteService _noteService;
    protected readonly IDialogService _dialogService;
    protected bool _isInVaultManager;

    public event EventHandler<string>? VaultOpened;

    protected VaultPageBase()
    {
        _noteService = App.NoteService;
        _dialogService = App.DialogService;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _isInVaultManager = e.Parameter is bool isManager && isManager;
    }

    protected void HandleVaultOpened(string path)
    {
        if (_isInVaultManager)
        {
            RaiseVaultOpened(path);
        }
        else
        {
            Frame.Navigate(typeof(WorkspacePage));
        }
    }

    protected void RaiseVaultOpened(string path)
    {
        VaultOpened?.Invoke(this, path);
    }

    protected async Task ShowVaultNotFoundErrorAsync()
    {
        await _dialogService.ShowErrorAsync("Vault folder no longer exists.", XamlRoot);
    }
}