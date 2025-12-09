namespace NoteForge.Models;

public class MatchingLine
{
    public string Text { get; set; } = string.Empty;
    public string SearchQuery { get; set; } = string.Empty;
    public bool IsLast { get; set; }
    public int LineNumber { get; set; }

    public MatchingLine(string text, string searchQuery, int lineNumber = 0, bool isLast = false)
    {
        Text = text;
        SearchQuery = searchQuery;
        LineNumber = lineNumber;
        IsLast = isLast;
    }
}
