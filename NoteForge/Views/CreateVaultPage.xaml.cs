using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using NoteForge.Services;
using Windows.Storage.Pickers;

namespace NoteForge.Views;

public sealed partial class CreateVaultPage : Page
{
    private readonly INoteService _noteService;
    private string? _selectedPath;

    public CreateVaultPage()
    {
        InitializeComponent();
        _noteService = App.NoteService;
    }

    private async void OnBrowseClicked(object sender, RoutedEventArgs e)
    {
        try 
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null)
            {
                _selectedPath = folder.Path;
                PathLabel.Text = _selectedPath;
                ValidateForm();
            }
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = $"Failed to pick folder: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private void OnVaultNameChanged(object sender, TextChangedEventArgs e)
    {
        ValidateForm();
    }

    private void ValidateForm()
    {
        CreateButton.IsEnabled = !string.IsNullOrWhiteSpace(VaultNameEntry.Text) && !string.IsNullOrEmpty(_selectedPath);
    }

    private async void OnCreateClicked(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedPath) || string.IsNullOrWhiteSpace(VaultNameEntry.Text))
            return;

        try
        {
            string fullPath = Path.Combine(_selectedPath, VaultNameEntry.Text);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            
            _noteService.SetVaultPath(fullPath);
            
            Frame.Navigate(typeof(WorkspacePage));
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = $"Failed to create vault: {ex.Message}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
    
    private void OnCancelClicked(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
        else
        {
            Frame.Navigate(typeof(VaultPage));
        }
    }

    private void OnVaultNameEntryLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var deleteButton = FindChild<Button>(textBox, "DeleteButton");
            if (deleteButton is not null)
            {
                deleteButton.Width = 0;
                deleteButton.Height = 0;
                deleteButton.Opacity = 0;
                deleteButton.IsHitTestVisible = false;
                deleteButton.Margin = new Thickness(0);
            }
        }
    }

    private static T? FindChild<T>(DependencyObject parent, string childName) where T : FrameworkElement
    {
        if (parent is null)
        {
            return null;
        }

        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T frameworkElement && frameworkElement.Name == childName)
            {
                return frameworkElement;
            }

            var result = FindChild<T>(child, childName);
            if (result is not null)
            {
                return result;
            }
        }
        return null;
    }
}

