using Microsoft.UI.Xaml;
using NoteForge.Services;

namespace NoteForge
{
    public partial class App : Application
    {
        public static Window MainWindow { get; private set; } = null!;
        public static INoteService NoteService { get; private set; } = null!;
        public static ITabManager TabManager { get; private set; } = null!;

        public App()
        {
            this.InitializeComponent();
            
            // Initialize Services
            NoteService = new NoteService();
            TabManager = new TabManager();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }
    }
}
