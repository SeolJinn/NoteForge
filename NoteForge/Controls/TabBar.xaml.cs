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
        TabScrollViewer.ViewChanged += OnScrollViewChanged;
        TabScrollViewer.Loaded += (s, e) => UpdateScrollIndicators();
        TabScrollViewer.SizeChanged += OnScrollViewerSizeChanged;
        TabsCollection.SizeChanged += OnTabsCollectionSizeChanged;
    }

    public void SetItemsSource(object itemsSource)
    {
        TabsCollection.ItemsSource = itemsSource;
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, UpdateScrollIndicators);
    }

    private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateScrollIndicators();
    }

    private void OnTabsCollectionSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateScrollIndicators();
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

    private void OnTabScrollViewerPointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(TabScrollViewer).Properties.MouseWheelDelta;
        var currentOffset = TabScrollViewer.HorizontalOffset;
        var newOffset = currentOffset - (delta * 0.5);
        TabScrollViewer.ChangeView(newOffset, null, null, false);
        e.Handled = true;
    }

    private void OnScrollViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        UpdateScrollIndicators();
    }

    private void UpdateScrollIndicators()
    {
        var canScrollLeft = TabScrollViewer.HorizontalOffset > 0;
        var canScrollRight = TabScrollViewer.HorizontalOffset < TabScrollViewer.ScrollableWidth;

        LeftScrollIndicator.Visibility = canScrollLeft ? Visibility.Visible : Visibility.Collapsed;
        RightScrollIndicator.Visibility = canScrollRight ? Visibility.Visible : Visibility.Collapsed;
    }
}