using System;
using System.IO;
using Microsoft.UI.Xaml;
using NoteForge.Views;

namespace NoteForge;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico"));
        RootFrame.Navigate(typeof(VaultPage));
    }
}