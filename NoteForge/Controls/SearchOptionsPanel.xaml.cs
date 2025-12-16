using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NoteForge.Controls;

public sealed partial class SearchOptionsPanel : UserControl
{
    public event EventHandler? PathFilterRequested;
    public event EventHandler? FileFilterRequested;

    public SearchOptionsPanel()
    {
        InitializeComponent();
    }

    private void OnPathFilterClicked(object sender, RoutedEventArgs e)
    {
        PathFilterRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnFileFilterClicked(object sender, RoutedEventArgs e)
    {
        FileFilterRequested?.Invoke(this, EventArgs.Empty);
    }
}