using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Interfaces;

namespace NoteForge.Handlers.Preview;

public class UpdatePreviewCommandHandler(IMarkdownPreviewService previewService)
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
        catch
        {
            return false;
        }
    }
}

public sealed record UpdatePreviewCommandRequest(WebView2? PreviewWebView, string Content) : IRequest<bool>;