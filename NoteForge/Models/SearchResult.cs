using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoteForge.Models;

public class SearchResult : INotifyPropertyChanged
{
    private bool _isExpanded;

    public Note Note { get; set; } = null!;
    public bool MatchesInTitle { get; set; }
    public List<MatchingLine> MatchingLines { get; set; } = new();
    public string SearchQuery { get; set; } = string.Empty;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShouldShowMatchingLines));
            }
        }
    }

    public int MatchCount => MatchingLines.Count;

    public bool ShouldShowMatchingLines => IsExpanded && MatchingLines.Count > 0;

    public SearchResult(Note note, string searchQuery = "")
    {
        Note = note;
        SearchQuery = searchQuery;
        _isExpanded = true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
