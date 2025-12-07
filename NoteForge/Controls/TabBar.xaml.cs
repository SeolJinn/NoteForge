using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Models;

namespace NoteForge.Controls;

public sealed partial class TabBar : UserControl
{
    public event EventHandler<Tab>? TabSelected;
    public event EventHandler<Tab>? TabClosed;
    public event EventHandler? NewTabRequested;

    public TabBar()
    {
        InitializeComponent();
    }

    public void SetItemsSource(object itemsSource)
    {
        TabsCollection.ItemsSource = itemsSource;
    }

    public void SetSelectedItem(object? item)
    {
        TabsCollection.SelectedItem = item;
    }

    private void OnTabSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is Tab tab)
        {
            TabSelected?.Invoke(this, tab);
        }
    }

    private void OnCloseTabClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is Tab tab)
        {
            TabClosed?.Invoke(this, tab);
        }
    }

    private void OnNewTabClicked(object sender, RoutedEventArgs e)
    {
        NewTabRequested?.Invoke(this, EventArgs.Empty);
    }
}