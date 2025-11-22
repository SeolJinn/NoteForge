namespace NoteForge.Models;

public class VaultInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTime LastAccessed { get; set; }
}