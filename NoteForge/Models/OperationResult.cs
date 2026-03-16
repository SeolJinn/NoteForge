namespace NoteForge.Models;

public sealed record OperationResult(bool Success, string? ErrorMessage = null)
{
    public static OperationResult Ok() => new(true);
    public static OperationResult Fail(string error) => new(false, error);
}
