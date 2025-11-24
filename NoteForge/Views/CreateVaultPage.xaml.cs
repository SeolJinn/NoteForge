using NoteForge.Services;
using CommunityToolkit.Maui.Storage;

namespace NoteForge.Views;

public partial class CreateVaultPage : ContentPage
{
    private readonly INoteService _noteService;
    private readonly ITabManager _tabManager;
    private string? _selectedPath;

    public CreateVaultPage(INoteService noteService, ITabManager tabManager)
    {
        InitializeComponent();
        _noteService = noteService;
        _tabManager = tabManager;
    }

    private async void OnBrowseClicked(object sender, EventArgs e)
    {
        try 
        {
            var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
            if (result.IsSuccessful)
            {
                _selectedPath = result.Folder.Path;
                PathLabel.Text = _selectedPath;
                ValidateForm();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to pick folder: {ex.Message}", "OK");
        }
    }

    private void ValidateForm()
    {
        CreateButton.IsEnabled = !string.IsNullOrWhiteSpace(VaultNameEntry.Text) && !string.IsNullOrEmpty(_selectedPath);
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedPath) || string.IsNullOrWhiteSpace(VaultNameEntry.Text))
            return;

        try
        {
            string fullPath = Path.Combine(_selectedPath, VaultNameEntry.Text);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            
            _noteService.SetVaultPath(fullPath);
            
            // Navigate to Main App
            Application.Current!.Windows[0].Page = new NavigationPage(new WorkspacePage(_noteService, _tabManager));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create vault: {ex.Message}", "OK");
        }
    }
}