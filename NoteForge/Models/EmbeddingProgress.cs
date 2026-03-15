namespace NoteForge.Models;

public class EmbeddingProgress
{
    public int TotalNotes { get; set; }
    public int ProcessedNotes { get; set; }
    public int SkippedNotes { get; set; }
    public int FailedNotes { get; set; }
    public string? CurrentNoteName { get; set; }
    public bool IsComplete { get; set; }

    public int ProgressPercentage =>
        TotalNotes > 0 ? (int)((float)ProcessedNotes / TotalNotes * 100) : 0;

    public string StatusText =>
        IsComplete
            ? $"Complete: {ProcessedNotes - FailedNotes} generated, {SkippedNotes} up-to-date, {FailedNotes} failed"
            : $"Processing {CurrentNoteName ?? "..."}";
}
