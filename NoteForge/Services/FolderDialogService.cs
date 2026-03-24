using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
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

    public Task<bool> ShowDeleteNoteDialogAsync(string noteName, XamlRoot xamlRoot)
    {
        var tcs = new TaskCompletionSource<bool>();

        var deleteButton = new Button
        {
            Content = "Delete",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 180, 40, 44)),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(0, 8, 0, 8)
        };
        deleteButton.Resources["ButtonBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 200, 50, 54));
        deleteButton.Resources["ButtonBackgroundPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 160, 35, 38));
        deleteButton.Resources["ButtonForegroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
        deleteButton.Resources["ButtonForegroundPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));

        var cancelButton = new Button
        {
            Content = "Cancel",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = (Brush)Application.Current.Resources["AppSurface"],
            Foreground = (Brush)Application.Current.Resources["TextPrimary"],
            CornerRadius = new CornerRadius(6),
            BorderBrush = (Brush)Application.Current.Resources["Separator"],
            BorderThickness = new Thickness(1),
            Padding = new Thickness(0, 8, 0, 8)
        };
        cancelButton.Resources["ButtonBackgroundPointerOver"] = Application.Current.Resources["Gray600"];
        cancelButton.Resources["ButtonBackgroundPressed"] = Application.Current.Resources["Separator"];

        var buttonGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            ColumnSpacing = 8
        };
        Grid.SetColumn(cancelButton, 0);
        Grid.SetColumn(deleteButton, 1);
        buttonGrid.Children.Add(cancelButton);
        buttonGrid.Children.Add(deleteButton);

        var content = new StackPanel
        {
            Spacing = 16,
            Width = 400,
            Children =
            {
                new TextBlock
                {
                    Text = "Delete Note",
                    FontSize = 18,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = (Brush)Application.Current.Resources["TextPrimary"]
                },
                new TextBlock
                {
                    Text = $"Are you sure you want to delete '{noteName}'? This action cannot be undone.",
                    FontSize = 13,
                    Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                    TextWrapping = TextWrapping.Wrap
                },
                buttonGrid
            }
        };

        var container = new Border
        {
            Background = (Brush)Application.Current.Resources["SideBar"],
            BorderBrush = (Brush)Application.Current.Resources["Separator"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(24),
            Child = content
        };

        var popup = new Popup
        {
            Child = container,
            IsLightDismissEnabled = true,
            LightDismissOverlayMode = LightDismissOverlayMode.On,
            XamlRoot = xamlRoot
        };

        var bounds = xamlRoot.Size;
        popup.HorizontalOffset = (bounds.Width - 448) / 2;
        popup.VerticalOffset = 100;

        popup.Closed += (s, e) => tcs.TrySetResult(false);
        deleteButton.Click += (s, e) => { tcs.TrySetResult(true); popup.IsOpen = false; };
        cancelButton.Click += (s, e) => { tcs.TrySetResult(false); popup.IsOpen = false; };

        popup.IsOpen = true;
        return tcs.Task;
    }
}