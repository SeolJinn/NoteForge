using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Models;

namespace NoteForge.Handlers;

public class GetNoteByPathQueryHandler : IRequestHandler<GetNoteByPathQueryRequest, Note?>
{
    public ValueTask<Note?> Handle(GetNoteByPathQueryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.FilePath) || !File.Exists(request.FilePath))
        {
            return ValueTask.FromResult<Note?>(null);
        }

        var note = new Note
        {
            FilePath = request.FilePath,
            Filename = Path.GetFileNameWithoutExtension(request.FilePath),
            Text = File.ReadAllText(request.FilePath),
            Date = File.GetLastWriteTime(request.FilePath)
        };

        return ValueTask.FromResult<Note?>(note);
    }
}

public sealed class GetNoteByPathQueryRequest(string filePath) : IRequest<Note?>
{
    public string FilePath { get; init; } = filePath;
}