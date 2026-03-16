using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Handlers.AI;

public class SummarizeNoteCommandHandler(IOllamaService ollamaService) : IRequestHandler<SummarizeNoteCommandRequest, IAsyncEnumerable<string>>
{
    private readonly IOllamaService _ollamaService = ollamaService;

    public ValueTask<IAsyncEnumerable<string>> Handle(SummarizeNoteCommandRequest request, CancellationToken cancellationToken)
    {
        var prompt = $"Please provide a concise summary of the following note:\n\n{request.Note.Text}";

        return ValueTask.FromResult(_ollamaService.StreamCompletionAsync(prompt, cancellationToken));
    }
}

public sealed class SummarizeNoteCommandRequest(Note note) : IRequest<IAsyncEnumerable<string>>
{
    public Note Note { get; init; } = note;
}