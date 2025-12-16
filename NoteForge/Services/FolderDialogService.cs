using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Interfaces;

namespace NoteForge.Services;

public class FolderDialogService : IFolderDialogService
{
    public async Task<string?> ShowCreateFolderDialogAsync(XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = "New Folder",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var nameBox = new TextBox
        {
            PlaceholderText = "Folder name",
            Margin = new Thickness(0, 8, 0, 0)
        };

        dialog.Content = nameBox;

        var result = await dialog.ShowAsync().AsTask();
        if (result is ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            return nameBox.Text;
        }

        return null;
    }

    public async Task<string?> ShowRenameFolderDialogAsync(string currentName, XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = "Rename Folder",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };

        var nameBox = new TextBox
        {
            Text = currentName,
            Margin = new Thickness(0, 8, 0, 0)
        };

        dialog.Content = nameBox;

        var result = await dialog.ShowAsync().AsTask();
        if (result is ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            return nameBox.Text;
        }

        return null;
    }

    public async Task<bool> ShowDeleteFolderDialogAsync(string folderName, XamlRoot xamlRoot)
    {
        var dialog = new ContentDialog
        {
            Title = "Delete Folder",
            Content = $"Are you sure you want to delete '{folderName}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync().AsTask();
        return result is ContentDialogResult.Primary;
    }
}