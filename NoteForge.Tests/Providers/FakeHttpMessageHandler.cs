using System.Net;

namespace NoteForge.Tests.Providers;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    public List<HttpRequestMessage> SentRequests { get; } = [];

    public void Enqueue(HttpResponseMessage response) => _responses.Enqueue(response);

    public void EnqueueText(HttpStatusCode status, string body, string contentType = "application/json")
    {
        var msg = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, contentType)
        };
        _responses.Enqueue(msg);
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);
        if (_responses.Count is 0)
        {
            throw new InvalidOperationException("No response queued.");
        }
        return Task.FromResult(_responses.Dequeue());
    }
}
