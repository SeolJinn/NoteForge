using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using NoteForge.Converters;
using NoteForge.Models;

namespace NoteForge.Controls;

public sealed partial class QuickFileNavigator : UserControl
{
    public event EventHandler<Note>? NoteSelected;

    private Popup? _popup;

    public QuickFileNavigator()
    {
        InitializeComponent();
    }

    public void Show(IEnumerable<Note> notes, XamlRoot xamlRoot)
    {
        var allNotes = notes.ToList();

        var searchBox = new AutoSuggestBox
        {
            PlaceholderText = "Type to search for a note...",
            Width = 400,
            QueryIcon = new SymbolIcon(Symbol.Find),
            Height = 36
        };

        searchBox.Resources["BoolToVisibilityConverter"] = new BoolToVisibilityConverter();
        searchBox.Resources["ControlCornerRadius"] = new CornerRadius(8);
        searchBox.Resources["OverlayCornerRadius"] = new CornerRadius(8);
        searchBox.Resources["TextControlBackground"] = Application.Current.Resources["AppSurface"];
        searchBox.Resources["TextControlBackgroundPointerOver"] = Application.Current.Resources["AppSurface"];
        searchBox.Resources["TextControlBackgroundFocused"] = Application.Current.Resources["AppSurface"];
        searchBox.Resources["TextControlForeground"] = Application.Current.Resources["TextPrimary"];
        searchBox.Resources["TextControlForegroundPointerOver"] = Application.Current.Resources["TextPrimary"];
        searchBox.Resources["TextControlForegroundFocused"] = Application.Current.Resources["TextPrimary"];
        searchBox.Resources["TextControlBorderBrush"] = Application.Current.Resources["Separator"];
        searchBox.Resources["TextControlBorderBrushPointerOver"] = Application.Current.Resources["TextSecondary"];
        searchBox.Resources["TextControlBorderBrushFocused"] = Application.Current.Resources["Primary"];
        searchBox.Resources["TextControlPlaceholderForeground"] = Application.Current.Resources["TextSecondary"];
        searchBox.Resources["TextControlPlaceholderForegroundPointerOver"] = Application.Current.Resources["TextSecondary"];
        searchBox.Resources["TextControlPlaceholderForegroundFocused"] = Application.Current.Resources["TextSecondary"];

        searchBox.ItemTemplate = (DataTemplate)XamlReader.Load(@"
            <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                <Grid Padding=""8,6"">
                    <TextBlock Text=""{Binding Filename}""
                               FontSize=""13""
                               Foreground=""#DADADA""
                               TextTrimming=""CharacterEllipsis""/>
                </Grid>
            </DataTemplate>");

        _popup = new Popup
        {
            Child = searchBox,
            IsLightDismissEnabled = true,
            LightDismissOverlayMode = LightDismissOverlayMode.On,
            XamlRoot = xamlRoot
        };

        var bounds = xamlRoot.Size;
        _popup.HorizontalOffset = (bounds.Width - 400) / 2;
        _popup.VerticalOffset = 100;

        searchBox.TextChanged += (s, args) =>
        {
            if (args.Reason is AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = searchBox.Text;
                var searchResults = App.SearchService.SearchByName(allNotes, query);
                searchBox.ItemsSource = searchResults.Take(10).ToList();
            }
        };

        searchBox.QuerySubmitted += (s, args) =>
        {
            Note? selectedNote = null;

            if (args.ChosenSuggestion is Note note)
            {
                selectedNote = note;
            }
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                var searchResults = App.SearchService.SearchByName(allNotes, args.QueryText);
                selectedNote = searchResults.FirstOrDefault();
            }

            if (selectedNote is not null)
            {
                NoteSelected?.Invoke(this, selectedNote);
                _popup.IsOpen = false;
            }
        };

        searchBox.PreviewKeyDown += (s, args) =>
        {
            if (args.Key is Windows.System.VirtualKey.Escape)
            {
                _popup.IsOpen = false;
                args.Handled = true;
            }
        };

        _popup.IsOpen = true;

        Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            searchBox.ItemsSource = allNotes.Take(10).ToList();
            searchBox.Focus(FocusState.Programmatic);
        });
    }
}