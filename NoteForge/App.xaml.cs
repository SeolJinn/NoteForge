using NoteForge.Services;
using NoteForge.Views;

namespace NoteForge
{
    public partial class App : Application
    {
        public App(IServiceProvider services)
        {
            InitializeComponent();
            Services = services;
        }
        
        public IServiceProvider Services { get; }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new NavigationPage(new VaultPage(Services.GetRequiredService<INoteService>())));
            
            //TODO: Add some sort of resizing mechanism for better UI pages

            return window;
        }
    }
}
