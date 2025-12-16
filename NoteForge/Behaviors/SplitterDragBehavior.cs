using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace NoteForge.Behaviors;

public static class SplitterDragBehavior
{
    private static bool s_isDragging;
    private static double s_startX;

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SplitterDragBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty TargetColumnProperty =
        DependencyProperty.RegisterAttached(
            "TargetColumn",
            typeof(ColumnDefinition),
            typeof(SplitterDragBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty TitleBarColumnProperty =
        DependencyProperty.RegisterAttached(
            "TitleBarColumn",
            typeof(ColumnDefinition),
            typeof(SplitterDragBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IndicatorProperty =
        DependencyProperty.RegisterAttached(
            "Indicator",
            typeof(Rectangle),
            typeof(SplitterDragBehavior),
            new PropertyMetadata(null));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static ColumnDefinition GetTargetColumn(DependencyObject obj) => (ColumnDefinition)obj.GetValue(TargetColumnProperty);
    public static void SetTargetColumn(DependencyObject obj, ColumnDefinition value) => obj.SetValue(TargetColumnProperty, value);

    public static ColumnDefinition GetTitleBarColumn(DependencyObject obj) => (ColumnDefinition)obj.GetValue(TitleBarColumnProperty);
    public static void SetTitleBarColumn(DependencyObject obj, ColumnDefinition value) => obj.SetValue(TitleBarColumnProperty, value);

    public static Rectangle GetIndicator(DependencyObject obj) => (Rectangle)obj.GetValue(IndicatorProperty);
    public static void SetIndicator(DependencyObject obj, Rectangle value) => obj.SetValue(IndicatorProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Border border)
            return;

        if ((bool)e.NewValue)
        {
            border.PointerEntered += OnPointerEntered;
            border.PointerExited += OnPointerExited;
            border.PointerPressed += OnPointerPressed;
            border.PointerMoved += OnPointerMoved;
            border.PointerReleased += OnPointerReleased;
        }
        else
        {
            border.PointerEntered -= OnPointerEntered;
            border.PointerExited -= OnPointerExited;
            border.PointerPressed -= OnPointerPressed;
            border.PointerMoved -= OnPointerMoved;
            border.PointerReleased -= OnPointerReleased;
        }
    }

    private static void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border border)
            return;

        var indicator = GetIndicator(border);
        if (indicator is not null)
        {
            indicator.Fill = (Brush)Application.Current.Resources["Primary"];
        }
    }

    private static void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border border)
            return;

        if (!s_isDragging)
        {
            var indicator = GetIndicator(border);
            if (indicator is not null)
            {
                indicator.Fill = (Brush)Application.Current.Resources["Separator"];
            }
        }
    }

    private static void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border border)
            return;

        var parent = FindParent<Page>(border);
        if (parent is null)
            return;

        s_isDragging = true;
        s_startX = e.GetCurrentPoint(parent).Position.X;
        border.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private static void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!s_isDragging || sender is not Border border)
            return;

        var parent = FindParent<Page>(border);
        if (parent is null)
            return;

        var targetColumn = GetTargetColumn(border);
        var titleBarColumn = GetTitleBarColumn(border);

        if (targetColumn is null || titleBarColumn is null)
            return;

        var currentPoint = e.GetCurrentPoint(parent).Position.X;
        var deltaX = currentPoint - s_startX;

        var currentWidth = targetColumn.ActualWidth;
        var newWidth = currentWidth + deltaX;

        var totalWidth = parent.ActualWidth;
        var maxWidth = totalWidth * 0.8;

        if (newWidth >= targetColumn.MinWidth && newWidth <= maxWidth && newWidth <= totalWidth - 200)
        {
            targetColumn.Width = new GridLength(newWidth);
            titleBarColumn.Width = new GridLength(newWidth);
            s_startX = currentPoint;
        }

        e.Handled = true;
    }

    private static void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!s_isDragging || sender is not Border border)
            return;

        s_isDragging = false;
        border.ReleasePointerCapture(e.Pointer);

        var indicator = GetIndicator(border);
        if (indicator is not null)
        {
            indicator.Fill = (Brush)Application.Current.Resources["Separator"];
        }

        e.Handled = true;
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent is not null)
        {
            if (parent is T typedParent)
                return typedParent;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}