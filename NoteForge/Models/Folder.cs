using System;
using System.Collections.ObjectModel;

namespace NoteForge.Models;

public partial class Folder : ObservableObject
{
    private string _name = string.Empty;
    private bool _isExpanded = true;
    private string _directoryPath = string.Empty;

    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string DirectoryPath
    {
        get => _directoryPath;
        set => SetProperty(ref _directoryPath, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public ObservableCollection<Folder> SubFolders { get; } = [];

    public ObservableCollection<Note> Notes { get; } = [];

    public bool IsEmpty => SubFolders.Count == 0 && Notes.Count == 0;
}