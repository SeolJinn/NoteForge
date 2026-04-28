using System.Net;
using NoteForge.Services.Ai;

namespace NoteForge.Tests.Providers;

public class GeminiAiProviderTests
{
    [Fact]
    public async Task GenerateEmbeddingAsync_returns_vector_from_response()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueText(HttpStatusCode.OK, "{\"embedding\":{\"values\":[0.4,0.5,0.6]}}");

        var provider = new GeminiAiProvider(new HttpClient(handler), apiKeyResolver: () => "test-key");

        var result = await provider.GenerateEmbeddingAsync("hello");

        Assert.NotNull(result);
        Assert.Equal([0.4f, 0.5f, 0.6f], result);
        var sent = handler.SentRequests.Single();
        Assert.Equal("test-key", sent.Headers.GetValues("x-goog-api-key").Single());
        Assert.DoesNotContain("key=", sent.RequestUri!.Query);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_throws_AiAuthException_on_403()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueText(HttpStatusCode.Forbidden, "{\"error\":\"bad key\"}");

        var provider = new GeminiAiProvider(new HttpClient(handler), apiKeyResolver: () => "bad");

        await Assert.ThrowsAsync<AiAuthException>(() => provider.GenerateEmbeddingAsync("hello"));
    }

    [Fact]
    public async Task StreamCompletionAsync_yields_text_parts()
    {
        var handler = new FakeHttpMessageHandler();
        var sse =
            "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Hi\"}]}}]}\n\n" +
            "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\" there\"}]}}]}\n\n";
        handler.EnqueueText(HttpStatusCode.OK, sse, "text/event-stream");

        var provider = new GeminiAiProvider(new HttpClient(handler), apiKeyResolver: () => "test-key");

        var collected = new List<string>();
        await foreach (var chunk in provider.StreamCompletionAsync("hi"))
        {
            collected.Add(chunk);
        }

        Assert.Equal(["Hi", " there"], collected);
    }
}
