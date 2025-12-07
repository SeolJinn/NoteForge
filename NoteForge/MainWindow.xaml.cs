using Microsoft.UI.Xaml;
using NoteForge.Views;

namespace NoteForge
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            
            RootFrame.Navigate(typeof(VaultPage));
        }
    }
}