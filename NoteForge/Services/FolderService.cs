using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NoteForge.Helpers;
using NoteForge.Interfaces;
using NoteForge.Models;
using Windows.Storage;

namespace NoteForge.Services;

public class FolderService : IFolderService
{
    private const string ExpandedFoldersKey = "ExpandedFolders";

    private readonly ApplicationDataContainer _localSettings;
    private readonly INoteService _noteService;
    private readonly ILogger<FolderService> _logger;

    public FolderService(INoteService noteService, ILogger<FolderService> logger)
    {
        _localSettings = ApplicationData.Current.LocalSettings;
        _noteService = noteService;
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read folder contents: {Path}", folder.DirectoryPath);
        }
    }

    public OperationResult CreateFolder(string parentPath, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return OperationResult.Fail("Folder name cannot be empty.");
        }

        var sanitizedName = SanitizeFolderName(name);
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return OperationResult.Fail("Folder name contains only invalid characters.");
        }

        var folderPath = Path.Combine(parentPath, sanitizedName);

        if (!PathValidator.IsWithinVault(folderPath, _noteService.CurrentNotebookPath))
        {
            return OperationResult.Fail("The folder path is outside the vault.");
        }

        if (Directory.Exists(folderPath))
        {
            return OperationResult.Fail("A folder with that name already exists.");
        }

        try
        {
            Directory.CreateDirectory(folderPath);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder: {Path}", folderPath);
            return OperationResult.Fail("Failed to create the folder. Check permissions.");
        }
    }

    public OperationResult RenameFolder(string folderPath, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return OperationResult.Fail("Folder name cannot be empty.");
        }

        if (!Directory.Exists(folderPath))
        {
            return OperationResult.Fail("The folder no longer exists.");
        }

        var sanitizedName = SanitizeFolderName(newName);
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return OperationResult.Fail("Folder name contains only invalid characters.");
        }

        var parentPath = Path.GetDirectoryName(folderPath);
        if (string.IsNullOrEmpty(parentPath))
        {
            return OperationResult.Fail("Could not determine parent directory.");
        }

        var newPath = Path.Combine(parentPath, sanitizedName);

        if (!PathValidator.IsWithinVault(newPath, _noteService.CurrentNotebookPath))
        {
            return OperationResult.Fail("The name contains invalid path characters.");
        }

        if (Directory.Exists(newPath))
        {
            return OperationResult.Fail("A folder with that name already exists.");
        }

        try
        {
            Directory.Move(folderPath, newPath);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rename folder from {OldPath} to {NewPath}", folderPath, newPath);
            return OperationResult.Fail("Failed to rename the folder. It may be in use.");
        }
    }

    public OperationResult DeleteFolder(string folderPath)
    {
        if (!PathValidator.IsWithinVault(folderPath, _noteService.CurrentNotebookPath))
        {
            return OperationResult.Fail("The folder path is outside the vault.");
        }

        if (!Directory.Exists(folderPath))
        {
            return OperationResult.Fail("The folder no longer exists.");
        }

        if (!IsFolderEmpty(folderPath))
        {
            return OperationResult.Fail("Cannot delete a non-empty folder.");
        }

        try
        {
            Directory.Delete(folderPath);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete folder: {Path}", folderPath);
            return OperationResult.Fail("Failed to delete the folder. It may be in use.");
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check if folder is empty: {Path}", folderPath);
            return false;
        }
    }

    public void SaveExpandedState(Folder rootFolder)
    {
        List<string> expandedPaths = [];
        CollectExpandedPaths(rootFolder, expandedPaths);

        try
        {
            var json = JsonSerializer.Serialize(expandedPaths);
            _localSettings.Values[ExpandedFoldersKey] = json;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save expanded folder state");
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
                if (expandedPaths is not null)
                {
                    ApplyExpandedState(rootFolder, new HashSet<string>(expandedPaths, StringComparer.OrdinalIgnoreCase));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load expanded folder state");
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
