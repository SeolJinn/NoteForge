using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Services;

namespace NoteForge.Handlers;

public sealed class CreateSectionCommandHandler(SectionService sectionService) : IRequestHandler<CreateSectionCommandRequest, bool>
{
    public ValueTask<bool> Handle(CreateSectionCommandRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValueTask.FromResult(false);
        }

        sectionService.AddSection(request.Name);
        return ValueTask.FromResult(true);
    }
}

public sealed record CreateSectionCommandRequest(string Name) : IRequest<bool>;
