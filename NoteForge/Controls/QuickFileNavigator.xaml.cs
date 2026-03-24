using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using NoteForge.Models;

namespace NoteForge.Controls;

public sealed partial class QuickFileNavigator : UserControl
{
    public event EventHandler<Note>? NoteSelected;

    private Popup? _popup;
    private List<Note> _allNotes = [];

    public QuickFileNavigator()
    {
        InitializeComponent();
    }

    public void Show(IEnumerable<Note> notes, XamlRoot xamlRoot)
    {
        _allNotes = [.. notes];

        var searchBox = new TextBox
        {
            PlaceholderText = "Type to search for a note...",
            Width = 400,
            Height = 36,
            CornerRadius = new CornerRadius(8),
            Background = (Brush)Application.Current.Resources["AppSurface"],
            Foreground = (Brush)Application.Current.Resources["TextPrimary"],
            BorderBrush = (Brush)Application.Current.Resources["Separator"],
            BorderThickness = new Thickness(1)
        };
        searchBox.Resources["TextControlBackgroundPointerOver"] = Application.Current.Resources["AppSurface"];
        searchBox.Resources["TextControlBackgroundFocused"] = Application.Current.Resources["AppSurface"];
        searchBox.Resources["TextControlForegroundPointerOver"] = Application.Current.Resources["TextPrimary"];
        searchBox.Resources["TextControlForegroundFocused"] = Application.Current.Resources["TextPrimary"];
        searchBox.Resources["TextControlBorderBrushPointerOver"] = Application.Current.Resources["TextSecondary"];
        searchBox.Resources["TextControlBorderBrushFocused"] = Application.Current.Resources["Primary"];

        var resultsList = new ListView
        {
            MaxHeight = 300,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            SelectionMode = ListViewSelectionMode.Single,
            IsItemClickEnabled = true
        };
        resultsList.Resources["ListViewItemBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 58, 58));
        resultsList.Resources["ListViewItemBackgroundSelected"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 58, 58));
        resultsList.Resources["ListViewItemBackgroundSelectedPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 68, 68, 68));
        resultsList.Resources["ListViewItemBackgroundPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 50, 50, 50));

        resultsList.ItemContainerStyle = new Style(typeof(ListViewItem));
        resultsList.ItemContainerStyle.Setters.Add(new Setter(ListViewItem.PaddingProperty, new Thickness(0)));
        resultsList.ItemContainerStyle.Setters.Add(new Setter(ListViewItem.MinHeightProperty, 0d));
        resultsList.ItemContainerStyle.Setters.Add(new Setter(ListViewItem.CornerRadiusProperty, new CornerRadius(6)));

        resultsList.ItemTemplate = CreateItemTemplate();
        resultsList.ItemClick += (s, e) =>
        {
            if (e.ClickedItem is Note note)
            {
                NoteSelected?.Invoke(this, note);
                _popup!.IsOpen = false;
            }
        };

        var emptyMessage = new TextBlock
        {
            Text = "No notes found",
            FontSize = 13,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"],
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(0, 16, 0, 16),
            Visibility = Visibility.Collapsed
        };

        var resultsContainer = new Border
        {
            Background = (Brush)Application.Current.Resources["SideBar"],
            BorderBrush = (Brush)Application.Current.Resources["Separator"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(4),
            Visibility = Visibility.Collapsed
        };

        var resultsPanel = new StackPanel();
        resultsPanel.Children.Add(resultsList);
        resultsPanel.Children.Add(emptyMessage);
        resultsContainer.Child = resultsPanel;

        var container = new StackPanel
        {
            Width = 400,
            Spacing = 4
        };
        container.Children.Add(searchBox);
        container.Children.Add(resultsContainer);

        _popup = new Popup
        {
            Child = container,
            IsLightDismissEnabled = true,
            LightDismissOverlayMode = LightDismissOverlayMode.On,
            XamlRoot = xamlRoot
        };

        var bounds = xamlRoot.Size;
        _popup.HorizontalOffset = (bounds.Width - 400) / 2;
        _popup.VerticalOffset = 100;

        searchBox.TextChanged += (s, e) =>
        {
            var query = searchBox.Text?.Trim() ?? string.Empty;
            List<Note> results;
            if (string.IsNullOrWhiteSpace(query))
                results = [.. _allNotes.Take(10)];
            else
                results = [.. App.SearchService.SearchByName(_allNotes, query).Take(10)];

            resultsList.ItemsSource = results;
            resultsList.Visibility = results.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            emptyMessage.Visibility = results.Count is 0 ? Visibility.Visible : Visibility.Collapsed;
            resultsContainer.Visibility = Visibility.Visible;
        };

        searchBox.PreviewKeyDown += (s, e) =>
        {
            if (e.Key is Windows.System.VirtualKey.Escape)
            {
                _popup.IsOpen = false;
                e.Handled = true;
            }
            else if (e.Key is Windows.System.VirtualKey.Enter)
            {
                var source = resultsList.ItemsSource as List<Note>;
                if (source?.Count > 0)
                {
                    NoteSelected?.Invoke(this, source[0]);
                    _popup.IsOpen = false;
                    e.Handled = true;
                }
            }
            else if (e.Key is Windows.System.VirtualKey.Down && resultsList.Items.Count > 0)
            {
                resultsList.Focus(FocusState.Programmatic);
                resultsList.SelectedIndex = 0;
                e.Handled = true;
            }
        };

        resultsList.PreviewKeyDown += (s, e) =>
        {
            if (e.Key is Windows.System.VirtualKey.Escape)
            {
                _popup.IsOpen = false;
                e.Handled = true;
            }
            else if (e.Key is Windows.System.VirtualKey.Enter && resultsList.SelectedItem is Note note)
            {
                NoteSelected?.Invoke(this, note);
                _popup.IsOpen = false;
                e.Handled = true;
            }
        };

        _popup.IsOpen = true;

        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(
            Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            var initial = (List<Note>)[.. _allNotes.Take(10)];
            resultsList.ItemsSource = initial;
            resultsList.Visibility = initial.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            emptyMessage.Visibility = initial.Count is 0 ? Visibility.Visible : Visibility.Collapsed;
            resultsContainer.Visibility = Visibility.Visible;
            searchBox.Focus(FocusState.Programmatic);
        });
    }

    private static DataTemplate CreateItemTemplate()
    {
        return (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(@"
            <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                <Grid Padding=""12,8"">
                    <TextBlock Text=""{Binding Filename}""
                               FontSize=""13""
                               Foreground=""#DADADA""
                               TextTrimming=""CharacterEllipsis""/>
                </Grid>
            </DataTemplate>");
    }
}
