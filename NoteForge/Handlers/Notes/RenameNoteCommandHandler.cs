using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Models;

namespace NoteForge.Handlers.Notes;

public class RenameNoteCommandHandler : IRequestHandler<RenameNoteCommandRequest, bool>
{
    public async ValueTask<bool> Handle(RenameNoteCommandRequest request, CancellationToken cancellationToken)
    {
        var note = request.Note;
        var newName = request.NewName;

        if (string.IsNullOrWhiteSpace(newName) || note is null || string.IsNullOrEmpty(note.FilePath))
        {
            return false;
        }

        if (!newName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            newName += ".md";
        }

        var directory = Path.GetDirectoryName(note.FilePath);
        if (string.IsNullOrEmpty(directory))
        {
            return false;
        }

        var newPath = Path.Combine(directory, newName);

        if (string.Equals(note.FilePath, newPath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (File.Exists(newPath))
        {
            return false;
        }

        try
        {
            var oldPath = note.FilePath;
            await Task.Run(() => File.Move(oldPath, newPath), cancellationToken);

            note.FilePath = newPath;
            note.Filename = Path.GetFileNameWithoutExtension(newPath);

            return true;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class RenameNoteCommandRequest(Note note, string newName) : IRequest<bool>
{
    public Note Note { get; init; } = note;
    public string NewName { get; init; } = newName;
}