using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers.Notes;

public class GetNotesQueryHandler(INoteService noteService, ILogger<GetNotesQueryHandler> logger) : IRequestHandler<GetNotesQueryRequest, IEnumerable<Note>>
{
    public async ValueTask<IEnumerable<Note>> Handle(GetNotesQueryRequest request, CancellationToken cancellationToken)
    {
        if (!noteService.IsConfigured)
        {
            return [];
        }

        List<Note> notes = [];

        try
        {
            var files = Directory.EnumerateFiles(noteService.CurrentNotebookPath, "*.md", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var note = await Note.FromFileAsync(file, cancellationToken);
                notes.Add(note);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load notes from {VaultPath}", noteService.CurrentNotebookPath);
        }

        return notes.OrderByDescending(n => n.Date);
    }
}

public sealed class GetNotesQueryRequest : IRequest<IEnumerable<Note>>;