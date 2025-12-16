using System.Collections.Generic;
using System.Linq;
using NoteForge.Models;

namespace NoteForge.Services;

public class FolderTreeService
{
    public Note? FindNoteInTree(Folder folder, string noteFilePath)
    {
        var note = folder.Notes.FirstOrDefault(n => n.FilePath == noteFilePath);
        if (note is not null)
        {
            return note;
        }

        foreach (var subfolder in folder.SubFolders)
        {
            note = FindNoteInTree(subfolder, noteFilePath);
            if (note is not null)
            {
                return note;
            }
        }

        return null;
    }

    public Dictionary<string, bool> GetExpandedStates(Folder rootFolder)
    {
        var states = new Dictionary<string, bool>();
        CollectExpandedStates(rootFolder, states);
        return states;
    }

    private void CollectExpandedStates(Folder folder, Dictionary<string, bool> states)
    {
        states[folder.DirectoryPath] = folder.IsExpanded;
        foreach (var subfolder in folder.SubFolders)
        {
            CollectExpandedStates(subfolder, states);
        }
    }

    public void RestoreExpandedStates(Folder rootFolder, Dictionary<string, bool> states)
    {
        ApplyExpandedStates(rootFolder, states);
    }

    private void ApplyExpandedStates(Folder folder, Dictionary<string, bool> states)
    {
        if (states.TryGetValue(folder.DirectoryPath, out var isExpanded))
        {
            folder.IsExpanded = isExpanded;
        }
        foreach (var subfolder in folder.SubFolders)
        {
            ApplyExpandedStates(subfolder, states);
        }
    }
}