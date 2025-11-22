using NoteForge.Models;
using CommunityToolkit.Maui.Storage;
using System.Text.Json;

namespace NoteForge.Services;

public interface INoteService
{
    string CurrentNotebookPath { get; }
    bool IsConfigured { get; }
    Task<IEnumerable<Note>> GetNotesAsync();
    Task<string?> PickFolderAsync();
    void SetVaultPath(string path);
    List<VaultInfo> GetRecentVaults();
}

public class NoteService : INoteService
{
    private const string VaultPathKey = "VaultPath";
    private const string RecentVaultsKey = "RecentVaults";
    
    public string CurrentNotebookPath { get; private set; }

    public bool IsConfigured => !string.IsNullOrEmpty(CurrentNotebookPath) && Directory.Exists(CurrentNotebookPath);

    public NoteService()
    {
        // Load from Preferences
        CurrentNotebookPath = Preferences.Get(VaultPathKey, string.Empty);
    }

    public async Task<IEnumerable<Note>> GetNotesAsync()
    {
        if (!IsConfigured)
        {
            return [];
        }

        var notes = new List<Note>();
        
        try 
        {
            var files = Directory.EnumerateFiles(CurrentNotebookPath, "*.md");

            foreach (var file in files)
            {
                var note = new Note
                {
                    Filename = Path.GetFileName(file),
                    FilePath = file,
                    Date = File.GetLastWriteTime(file),
                    Text = await File.ReadAllTextAsync(file)
                };
                notes.Add(note);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading notes: {ex.Message}");
        }

        return notes;
    }

    public async Task<string?> PickFolderAsync()
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
            if (result.IsSuccessful)
            {
                SetVaultPath(result.Folder.Path);
                return CurrentNotebookPath;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error picking folder: {ex.Message}");
        }

        return null;
    }

    public void SetVaultPath(string path)
    {
        CurrentNotebookPath = path;
        Preferences.Set(VaultPathKey, path);
        AddToRecentVaults(path);
    }

    public List<VaultInfo> GetRecentVaults()
    {
        string json = Preferences.Get(RecentVaultsKey, "[]");

        try
        {
            return JsonSerializer.Deserialize<List<VaultInfo>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void AddToRecentVaults(string path)
    {
        var recent = GetRecentVaults();
        
        // Remove existing entry if present (to update timestamp/position)
        recent.RemoveAll(v => v.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

        // Add to top
        recent.Insert(0, new VaultInfo 
        { 
            Name = new DirectoryInfo(path).Name, 
            Path = path, 
            LastAccessed = DateTime.Now 
        });

        // Keep only last 5
        if (recent.Count > 5)
        {
            recent = [.. recent.Take(5)];
        }

        string json = JsonSerializer.Serialize(recent);
        Preferences.Set(RecentVaultsKey, json);
    }
}
