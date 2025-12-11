namespace NoteForge.Models;

public class MatchingLine(string text, string searchQuery, int lineNumber = 0, bool isLast = false)
{
    public string Text { get; set; } = text;
    public string SearchQuery { get; set; } = searchQuery;
    public bool IsLast { get; set; } = isLast;
    public int LineNumber { get; set; } = lineNumber;
}