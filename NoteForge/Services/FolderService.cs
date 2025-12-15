using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NoteForge.Models;
using Windows.Storage;

namespace NoteForge.Services;

public class FolderService
{
    private const string ExpandedFoldersKey = "ExpandedFolders";

    private readonly ApplicationDataContainer _localSettings;

    public FolderService()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
    }

    public Folder BuildFolderTree(string vaultPath)
    {
        var rootFolder = new Folder
        {
            Name = Path.GetFileName(vaultPath),
            DirectoryPath = vaultPath,
            IsExpanded = true
        };

        BuildFolderTreeRecursive(rootFolder);
        return rootFolder;
    }

    private void BuildFolderTreeRecursive(Folder folder)
    {
        if (!Directory.Exists(folder.DirectoryPath))
        {
            return;
        }

        try
        {
            var directories = Directory.EnumerateDirectories(folder.DirectoryPath)
                .OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase);

            foreach (var dir in directories)
            {
                var subfolder = new Folder
                {
                    Name = Path.GetFileName(dir),
                    DirectoryPath = dir,
                    IsExpanded = false
                };

                folder.SubFolders.Add(subfolder);
                BuildFolderTreeRecursive(subfolder);
            }

            var files = Directory.EnumerateFiles(folder.DirectoryPath, "*.md")
                .OrderBy(f => Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                var note = Note.FromFile(file);
                folder.Notes.Add(note);
            }
        }
        catch
        {
        }
    }

    public bool CreateFolder(string parentPath, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var sanitizedName = SanitizeFolderName(name);
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return false;
        }

        var folderPath = Path.Combine(parentPath, sanitizedName);

        if (Directory.Exists(folderPath))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(folderPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool RenameFolder(string folderPath, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || !Directory.Exists(folderPath))
        {
            return false;
        }

        var sanitizedName = SanitizeFolderName(newName);
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return false;
        }

        var parentPath = Path.GetDirectoryName(folderPath);
        if (string.IsNullOrEmpty(parentPath))
        {
            return false;
        }

        var newPath = Path.Combine(parentPath, sanitizedName);

        if (Directory.Exists(newPath))
        {
            return false;
        }

        try
        {
            Directory.Move(folderPath, newPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return false;
        }

        if (!IsFolderEmpty(folderPath))
        {
            return false;
        }

        try
        {
            Directory.Delete(folderPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsFolderEmpty(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return true;
        }

        try
        {
            return !Directory.EnumerateFileSystemEntries(folderPath).Any();
        }
        catch
        {
            return false;
        }
    }

    public void SaveExpandedState(Folder rootFolder)
    {
        var expandedPaths = new List<string>();
        CollectExpandedPaths(rootFolder, expandedPaths);

        try
        {
            var json = JsonSerializer.Serialize(expandedPaths);
            _localSettings.Values[ExpandedFoldersKey] = json;
        }
        catch
        {
        }
    }

    private void CollectExpandedPaths(Folder folder, List<string> expandedPaths)
    {
        if (folder.IsExpanded && !string.IsNullOrEmpty(folder.DirectoryPath))
        {
            expandedPaths.Add(folder.DirectoryPath);
        }

        foreach (var subfolder in folder.SubFolders)
        {
            CollectExpandedPaths(subfolder, expandedPaths);
        }
    }

    public void LoadExpandedState(Folder rootFolder)
    {
        try
        {
            if (_localSettings.Values.TryGetValue(ExpandedFoldersKey, out var value) && value is string json)
            {
                var expandedPaths = JsonSerializer.Deserialize<List<string>>(json);
                if (expandedPaths != null)
                {
                    ApplyExpandedState(rootFolder, new HashSet<string>(expandedPaths, StringComparer.OrdinalIgnoreCase));
                }
            }
        }
        catch
        {
        }
    }

    private void ApplyExpandedState(Folder folder, HashSet<string> expandedPaths)
    {
        if (!string.IsNullOrEmpty(folder.DirectoryPath))
        {
            folder.IsExpanded = expandedPaths.Contains(folder.DirectoryPath);
        }

        foreach (var subfolder in folder.SubFolders)
        {
            ApplyExpandedState(subfolder, expandedPaths);
        }
    }

    public string GetNewPath(string oldPath, string newParentPath)
    {
        var fileName = Path.GetFileName(oldPath);
        return Path.Combine(newParentPath, fileName);
    }

    private string SanitizeFolderName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Trim();
    }
}
