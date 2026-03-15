using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoteForge.Models;

public partial class SearchResult(Note note, string searchQuery = "") : INotifyPropertyChanged
{
    private bool _isExpanded = true;

    public Note Note { get; set; } = note;
    public bool MatchesInTitle { get; set; }
    public List<MatchingLine> MatchingLines { get; set; } = [];
    public string SearchQuery { get; set; } = searchQuery;
    public float RelevanceScore { get; set; }

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

    public string RelevancePercentage => $"{(int)(RelevanceScore * 100)}%";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}