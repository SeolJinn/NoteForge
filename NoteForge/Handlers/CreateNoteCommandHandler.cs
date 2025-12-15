using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers;

public class CreateNoteCommandHandler(INoteService noteService) : IRequestHandler<CreateNoteCommandRequest, Note?>
{
    public async ValueTask<Note?> Handle(CreateNoteCommandRequest request, CancellationToken cancellationToken)
    {
        if (!noteService.IsConfigured)
        {
            return null;
        }

        try
        {
            var targetPath = request.TargetFolderPath ?? noteService.CurrentNotebookPath;

            string baseName = "Untitled";
            string fileName = baseName + ".md";
            string filePath = Path.Combine(targetPath, fileName);

            int counter = 1;
            while (File.Exists(filePath))
            {
                fileName = $"{baseName} {counter}.md";
                filePath = Path.Combine(targetPath, fileName);
                counter++;
            }

            await File.WriteAllTextAsync(filePath, string.Empty, cancellationToken);

            return new Note
            {
                Filename = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath,
                Date = DateTime.Now,
                Text = string.Empty
            };
        }
        catch
        {
            return null;
        }
    }
}

public sealed record CreateNoteCommandRequest(string? TargetFolderPath = null) : IRequest<Note?>;