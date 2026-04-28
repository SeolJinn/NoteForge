using System.Net;
using NoteForge.Services.Ai;

namespace NoteForge.Tests.Providers;

public class OpenAiAiProviderTests
{
    [Fact]
    public async Task GenerateEmbeddingAsync_returns_vector_from_response()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueText(HttpStatusCode.OK, "{\"data\":[{\"embedding\":[0.1,0.2,0.3]}]}");

        var provider = new OpenAiAiProvider(new HttpClient(handler), apiKeyResolver: () => "test-key");

        var result = await provider.GenerateEmbeddingAsync("hello");

        Assert.NotNull(result);
        Assert.Equal([0.1f, 0.2f, 0.3f], result);
        var sent = handler.SentRequests.Single();
        Assert.Equal("Bearer test-key", sent.Headers.Authorization!.ToString());
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_throws_AiAuthException_on_401()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueText(HttpStatusCode.Unauthorized, "{\"error\":\"bad key\"}");

        var provider = new OpenAiAiProvider(new HttpClient(handler), apiKeyResolver: () => "bad");

        await Assert.ThrowsAsync<AiAuthException>(() => provider.GenerateEmbeddingAsync("hello"));
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_throws_AiAuthException_when_key_missing()
    {
        var handler = new FakeHttpMessageHandler();
        var provider = new OpenAiAiProvider(new HttpClient(handler), apiKeyResolver: () => null);

        await Assert.ThrowsAsync<AiAuthException>(() => provider.GenerateEmbeddingAsync("hello"));
        Assert.Empty(handler.SentRequests);
    }

    [Fact]
    public async Task StreamCompletionAsync_yields_delta_content_chunks()
    {
        var handler = new FakeHttpMessageHandler();
        var sse = "data: {\"choices\":[{\"delta\":{\"content\":\"Hello\"}}]}\n\n" +
                  "data: {\"choices\":[{\"delta\":{\"content\":\" world\"}}]}\n\n" +
                  "data: [DONE]\n\n";
        handler.EnqueueText(HttpStatusCode.OK, sse, "text/event-stream");

        var provider = new OpenAiAiProvider(new HttpClient(handler), apiKeyResolver: () => "test-key");

        var collected = new List<string>();
        await foreach (var chunk in provider.StreamCompletionAsync("hi"))
        {
            collected.Add(chunk);
        }

        Assert.Equal(["Hello", " world"], collected);
    }
}
