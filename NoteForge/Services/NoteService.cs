using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NoteForge.Models;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace NoteForge.Services;

public interface INoteService
{
    string CurrentNotebookPath { get; }
    bool IsConfigured { get; }
    Task<IEnumerable<Note>> GetNotesAsync();
    Task<string?> PickFolderAsync();
    void SetVaultPath(string path);
    List<VaultInfo> GetRecentVaults();
    Task SaveNoteAsync(Note note);
    Task<bool> RenameNoteAsync(Note note, string newName);
}

public class NoteService : INoteService
{
    private const string VaultPathKey = "VaultPath";
    private const string RecentVaultsKey = "RecentVaults";
    
    public string CurrentNotebookPath { get; private set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrEmpty(CurrentNotebookPath) && Directory.Exists(CurrentNotebookPath);

    public NoteService()
    {
        // Load from LocalSettings
        CurrentNotebookPath = GetSetting(VaultPathKey, string.Empty);
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
                    Filename = Path.GetFileNameWithoutExtension(file),
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
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add("*");

            if (App.MainWindow is not null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            }

            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null)
            {
                SetVaultPath(folder.Path);
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
        SetSetting(VaultPathKey, path);
        AddToRecentVaults(path);
    }

    public List<VaultInfo> GetRecentVaults()
    {
        string json = GetSetting(RecentVaultsKey, "[]");

        try
        {
            return JsonSerializer.Deserialize<List<VaultInfo>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task SaveNoteAsync(Note note)
    {
        if (string.IsNullOrEmpty(note.FilePath))
        {
            return;
        }

        try
        {
            await File.WriteAllTextAsync(note.FilePath, note.Text);
        }
        catch (Exception ex)
        {
             System.Diagnostics.Debug.WriteLine($"Error saving note: {ex.Message}");
        }
    }

    public async Task<bool> RenameNoteAsync(Note note, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || note is null || string.IsNullOrEmpty(note.FilePath))
        {
            return false;
        }

        if (!newName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            newName += ".md";

        var directory = Path.GetDirectoryName(note.FilePath);
        if (string.IsNullOrEmpty(directory)) return false;

        var newPath = Path.Combine(directory, newName);

        if (string.Equals(note.FilePath, newPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (File.Exists(newPath))
        {
            return false;
        }

        try
        {
            File.Move(note.FilePath, newPath);
            
            note.FilePath = newPath;
            note.Filename = Path.GetFileNameWithoutExtension(newPath);
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error renaming note: {ex.Message}");
            return false;
        }
    }

    private void AddToRecentVaults(string path)
    {
        var recent = GetRecentVaults();
        
        recent.RemoveAll(v => v.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

        recent.Insert(0, new VaultInfo 
        { 
            Name = new DirectoryInfo(path).Name, 
            Path = path, 
            LastAccessed = DateTime.Now 
        });

        if (recent.Count > 5)
        {
            recent = [.. recent.Take(5)];
        }

        string json = JsonSerializer.Serialize(recent);
        SetSetting(RecentVaultsKey, json);
    }

    private static void SetSetting(string key, string value)
    {
        var localSettings = ApplicationData.Current.LocalSettings;
        localSettings.Values[key] = value;
    }

    private static string GetSetting(string key, string defaultValue)
    {
        var localSettings = ApplicationData.Current.LocalSettings;
        return localSettings.Values.TryGetValue(key, out var value) ? (string)value : defaultValue;
    }
}

