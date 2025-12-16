using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using NoteForge.Models;

namespace NoteForge.Handlers.AI;

public sealed class GenerateInlineAiSummaryCommandHandler(
    IMediator mediator,
    ILogger<GenerateInlineAiSummaryCommandHandler> logger)
    : IRequestHandler<GenerateInlineAiSummaryCommandRequest, bool>
{
    public async ValueTask<bool> Handle(GenerateInlineAiSummaryCommandRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Note.Text))
        {
            return false;
        }

        var dispatcher = DispatcherQueue.GetForCurrentThread();
        if (dispatcher is null)
        {
            return false;
        }

        dispatcher.TryEnqueue(() => request.OnSummaryStarted?.Invoke());

        try
        {
            var stream = await mediator.Send(new SummarizeNoteCommandRequest(request.Note), cancellationToken);
            var started = false;

            await foreach (var token in stream.WithCancellation(cancellationToken))
            {
                dispatcher.TryEnqueue(() =>
                {
                    if (!started)
                    {
                        request.OnFirstToken?.Invoke();
                        started = true;
                    }
                    request.OnTokenReceived?.Invoke(token);
                });
            }

            dispatcher.TryEnqueue(() => request.OnSummaryCompleted?.Invoke());

            return true;
        }
        catch (OperationCanceledException)
        {
            dispatcher.TryEnqueue(() => request.OnSummaryCancelled?.Invoke());
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate inline AI summary");
            dispatcher.TryEnqueue(() =>
            {
                request.OnSummaryFailed?.Invoke(ex.Message);
            });
            return false;
        }
    }
}

public sealed record GenerateInlineAiSummaryCommandRequest(
    Note Note,
    Action? OnSummaryStarted = null,
    Action? OnFirstToken = null,
    Action<string>? OnTokenReceived = null,
    Action? OnSummaryCompleted = null,
    Action? OnSummaryCancelled = null,
    Action<string>? OnSummaryFailed = null) : IRequest<bool>;
