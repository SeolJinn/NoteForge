using System;

namespace NoteForge.Models;

public class EmbeddingRecord
{
    public string FilePath { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = [];
    public string ContentHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
