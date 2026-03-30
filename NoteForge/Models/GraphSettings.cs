namespace NoteForge.Models;

public class GraphSettings
{
    public float SemanticThreshold { get; set; } = 0.1f;
    public float TfidfThreshold { get; set; } = 0.1f;
    public bool ShowExplicitLinks { get; set; } = true;
    public bool ShowSemanticLinks { get; set; } = true;
    public float RepulsionStrength { get; set; } = 100f;
    public float AttractionStrength { get; set; } = 0.01f;
    public float CenterGravity { get; set; } = 0.05f;
}
