namespace NoteForge.Models;

public enum EdgeType
{
    Explicit,
    Semantic,
    Hybrid
}

public partial class GraphEdge : ObservableObject
{
    private bool _isVisible = true;

    public GraphNode Source { get; init; } = null!;
    public GraphNode Target { get; init; } = null!;
    public EdgeType Type { get; init; }
    public float Strength { get; init; }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public string EdgeTypeLabel => Type.ToString();
}
