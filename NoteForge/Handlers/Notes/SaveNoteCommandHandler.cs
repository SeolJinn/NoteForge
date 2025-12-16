using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Models;

namespace NoteForge.Handlers.Notes;

public class SaveNoteCommandHandler : IRequestHandler<SaveNoteCommandRequest, bool>
{
    public async ValueTask<bool> Handle(SaveNoteCommandRequest request, CancellationToken cancellationToken)
    {
        if (request.Note is null || string.IsNullOrEmpty(request.Note.FilePath))
        {
            return false;
        }

        try
        {
            await File.WriteAllTextAsync(request.Note.FilePath, request.Note.Text, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class SaveNoteCommandRequest(Note note) : IRequest<bool>
{
    public Note Note { get; init; } = note;
}