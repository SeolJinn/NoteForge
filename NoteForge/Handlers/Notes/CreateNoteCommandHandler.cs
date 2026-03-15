using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using NoteForge.Interfaces;
using NoteForge.Models;
using NoteForge.Services.Search;

namespace NoteForge.Handlers.Notes;

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

            var note = new Note
            {
                Filename = Path.GetFileNameWithoutExtension(filePath),
                FilePath = filePath,
                Date = DateTime.Now,
                Text = string.Empty
            };

            App.Services.GetRequiredService<SemanticSearchStrategy>().InvalidateIndex();

            if (App.EmbeddingService is not null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await App.EmbeddingService.GenerateEmbeddingForNoteAsync(note, cancellationToken);
                        App.Services.GetRequiredService<SemanticSearchStrategy>().InvalidateEmbeddingsCache();
                    }
                    catch (Exception)
                    {
                    }
                }, cancellationToken);
            }

            return note;
        }
        catch
        {
            return null;
        }
    }
}

public sealed record CreateNoteCommandRequest(string? TargetFolderPath = null) : IRequest<Note?>;