using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Models;
using NoteForge.Services;

namespace NoteForge.Handlers.Notes;

public sealed class ToggleFavoriteCommandHandler(SectionService sectionService) : IRequestHandler<ToggleFavoriteCommandRequest, bool>
{
    public ValueTask<bool> Handle(ToggleFavoriteCommandRequest request, CancellationToken cancellationToken)
    {
        var isFavorite = SectionService.IsFavorite(request.Note.FilePath);

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
