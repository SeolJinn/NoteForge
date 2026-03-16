using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers.Notes;

public sealed class ToggleFavoriteCommandHandler(ISectionService sectionService) : IRequestHandler<ToggleFavoriteCommandRequest, bool>
{
    public ValueTask<bool> Handle(ToggleFavoriteCommandRequest request, CancellationToken cancellationToken)
    {
        var isFavorite = sectionService.IsFavorite(request.Note.FilePath);

        if (isFavorite)
        {
            sectionService.RemoveFavorite(request.Note.FilePath);
        }
        else
        {
            sectionService.AddFavorite(request.Note.FilePath);
        }

        return ValueTask.FromResult(!isFavorite);
    }
}

public sealed record ToggleFavoriteCommandRequest(Note Note) : IRequest<bool>;
