using System;

namespace NoteForge.Models;

public partial class Tab : ObservableObject
{
    private bool _isDirty;
    private bool _isActive;
    private bool _isNewTab;
    private string _displayName = string.Empty;

    public string Id { get; } = Guid.NewGuid().ToString();

    public string FilePath { get; set; } = string.Empty;

    public bool IsNewTab
    {
        get => _isNewTab;
        set => SetProperty(ref _isNewTab, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public bool IsDirty
    {
        get => _isDirty;
        set => SetProperty(ref _isDirty, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}