namespace NoteForge.Models;

public class Note
{
    public string Filename { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string FilePath { get; set; } = string.Empty;
}