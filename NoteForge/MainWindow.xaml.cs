using Microsoft.UI.Xaml;
using NoteForge.Views;

namespace NoteForge;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        AppWindow.SetIcon("Assets/app.ico");
        RootFrame.Navigate(typeof(VaultPage));
    }
}