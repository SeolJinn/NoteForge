using System.Collections.ObjectModel;

namespace NoteForge.Models;

public partial class GraphData : ObservableObject
{
    public ObservableCollection<GraphNode> Nodes { get; init; } = [];
    public ObservableCollection<GraphEdge> Edges { get; init; } = [];

    public void Clear()
    {
        Nodes.Clear();
        Edges.Clear();
    }
}
