using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Interfaces;

namespace NoteForge.Handlers.Preview;

public class UpdatePreviewCommandHandler(IMarkdownPreviewService previewService, ILogger<UpdatePreviewCommandHandler> logger)
    : IRequestHandler<UpdatePreviewCommandRequest, bool>
{
    public async ValueTask<bool> Handle(UpdatePreviewCommandRequest request, CancellationToken cancellationToken)
    {
        if (request.PreviewWebView is null || string.IsNullOrEmpty(request.Content))
        {
            return false;
        }

        try
        {
            if (request.PreviewWebView.CoreWebView2 is null)
            {
                await request.PreviewWebView.EnsureCoreWebView2Async().AsTask(cancellationToken: cancellationToken);
            }

            var html = previewService.ConvertToHtml(request.Content);
            request.PreviewWebView.NavigateToString(previewService.WrapInHtmlDocument(html));
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update markdown preview");
            return false;
        }
    }
}

public sealed record UpdatePreviewCommandRequest(WebView2? PreviewWebView, string Content) : IRequest<bool>;