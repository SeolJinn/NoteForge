using System.Collections.ObjectModel;

namespace NoteForge.Models;

public partial class NoteSection : ObservableObject
{
    private string _name = string.Empty;
    private bool _isExpanded = true;
    private bool _isVisible = true;

    public string Id { get; set; } = string.Empty;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public bool IsBuiltIn { get; set; }

    public ObservableCollection<Note> Notes { get; } = [];
}
