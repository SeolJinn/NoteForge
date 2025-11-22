using NoteForge.Services;

namespace NoteForge
{
    public partial class MainPage : ContentPage
    {
        private readonly INoteService _noteService;

        public MainPage(INoteService noteService)
        {
            InitializeComponent();
            _noteService = noteService;
            Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object? sender, EventArgs e)
        {
            await LoadNotes();
        }

        private async Task LoadNotes()
        {
            // Ensure we have a configured path
            if (!_noteService.IsConfigured)
            {
                PathLabel.Text = "No vault selected";
                NotesCollection.ItemsSource = null;
                return;
            }

            PathLabel.Text = $"Path: {_noteService.CurrentNotebookPath}";
            var notes = await _noteService.GetNotesAsync();
            NotesCollection.ItemsSource = notes;
        }

        private async void OnChangeFolderClicked(object sender, EventArgs e)
        {
            var newPath = await _noteService.PickFolderAsync();
            if (newPath is not null)
            {
                await LoadNotes();
            }
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await LoadNotes();
        }
    }
}
