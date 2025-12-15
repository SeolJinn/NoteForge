using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers;

public class GetNotesQueryHandler(INoteService noteService) : IRequestHandler<GetNotesQueryRequest, IEnumerable<Note>>
{
    public async ValueTask<IEnumerable<Note>> Handle(GetNotesQueryRequest request, CancellationToken cancellationToken)
    {
        if (!noteService.IsConfigured)
        {
            return [];
        }

        var notes = new List<Note>();

        try
        {
            var files = Directory.EnumerateFiles(noteService.CurrentNotebookPath, "*.md", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var note = await Note.FromFileAsync(file, cancellationToken);
                notes.Add(note);
            }
        }
        catch
        {
        }

        return notes.OrderByDescending(n => n.Date);
    }
}

public sealed class GetNotesQueryRequest : IRequest<IEnumerable<Note>>;