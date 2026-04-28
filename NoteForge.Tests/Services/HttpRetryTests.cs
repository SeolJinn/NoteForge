using System.Net;
using System.Net.Http.Headers;
using NoteForge.Services.Ai.Internal;
using NoteForge.Tests.Providers;

namespace NoteForge.Tests.Services;

public class HttpRetryTests
{
    [Fact]
    public async Task Returns_response_immediately_on_2xx_no_retry()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueText(HttpStatusCode.OK, "ok");
        var client = new HttpClient(handler);

        var response = await CallWithNoOpDelay(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task Returns_response_immediately_on_4xx_other_than_429_no_retry()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueText(HttpStatusCode.BadRequest, "bad");
        var client = new HttpClient(handler);

        var response = await CallWithNoOpDelay(client);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(handler.SentRequests);
    }

    [Fact]
    public async Task Retries_once_on_429()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueText(HttpStatusCode.TooManyRequests, "rate limited");
        handler.EnqueueText(HttpStatusCode.OK, "fine now");
        var client = new HttpClient(handler);

        var response = await CallWithNoOpDelay(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.SentRequests.Count);
    }

    [Fact]
    public async Task Retries_once_on_500()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueText(HttpStatusCode.InternalServerError, "boom");
        handler.EnqueueText(HttpStatusCode.OK, "fine");
        var client = new HttpClient(handler);

        var response = await CallWithNoOpDelay(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.SentRequests.Count);
    }

    [Fact]
    public async Task Does_not_retry_twice_on_persistent_429()
    {
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueText(HttpStatusCode.TooManyRequests, "still rate limited");
        handler.EnqueueText(HttpStatusCode.TooManyRequests, "still");
        var client = new HttpClient(handler);

        var response = await CallWithNoOpDelay(client);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(2, handler.SentRequests.Count);
    }

    [Fact]
    public void ShouldRetry_status_classification()
    {
        Assert.False(HttpRetry.ShouldRetry(new HttpResponseMessage(HttpStatusCode.OK)));
        Assert.False(HttpRetry.ShouldRetry(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        Assert.False(HttpRetry.ShouldRetry(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        Assert.True(HttpRetry.ShouldRetry(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        Assert.True(HttpRetry.ShouldRetry(new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        Assert.True(HttpRetry.ShouldRetry(new HttpResponseMessage(HttpStatusCode.BadGateway)));
        Assert.True(HttpRetry.ShouldRetry(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        Assert.True(HttpRetry.ShouldRetry(new HttpResponseMessage(HttpStatusCode.GatewayTimeout)));
    }

    [Fact]
    public void ResolveRetryDelay_uses_delta_when_present()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5));

        Assert.Equal(TimeSpan.FromSeconds(5), HttpRetry.ResolveRetryDelay(response));
    }

    [Fact]
    public void ResolveRetryDelay_uses_date_when_in_future()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        var future = DateTimeOffset.UtcNow.AddSeconds(10);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(future);

        var delay = HttpRetry.ResolveRetryDelay(response);

        Assert.True(delay.TotalSeconds >= 8 && delay.TotalSeconds <= 11);
    }

    [Fact]
    public void ResolveRetryDelay_falls_back_to_one_second_when_no_header()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        Assert.Equal(TimeSpan.FromSeconds(1), HttpRetry.ResolveRetryDelay(response));
    }

    [Fact]
    public void ResolveRetryDelay_caps_at_30_seconds()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromMinutes(10));

        Assert.Equal(TimeSpan.FromSeconds(30), HttpRetry.ResolveRetryDelay(response));
    }

    [Fact]
    public void ResolveRetryDelay_falls_back_when_date_in_past()
    {
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        var past = DateTimeOffset.UtcNow.AddMinutes(-5);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(past);

        Assert.Equal(TimeSpan.FromSeconds(1), HttpRetry.ResolveRetryDelay(response));
    }

    [Fact]
    public async Task Honors_retry_after_via_delay_function()
    {
        var handler = new FakeHttpMessageHandler();
        var resp429 = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("rate limited")
        };
        resp429.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(7));
        handler.Enqueue(resp429);
        handler.EnqueueText(HttpStatusCode.OK, "ok");

        TimeSpan? observedDelay = null;
        Func<TimeSpan, CancellationToken, Task> recordingDelay = (delay, ct) =>
        {
            observedDelay = delay;
            return Task.CompletedTask;
        };

        var client = new HttpClient(handler);
        var response = await HttpRetry.SendWithRetryAsync(
            client,
            () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"),
            HttpCompletionOption.ResponseContentRead,
            recordingDelay,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(7), observedDelay);
    }

    private static Task<HttpResponseMessage> CallWithNoOpDelay(HttpClient client) =>
        HttpRetry.SendWithRetryAsync(
            client,
            () => new HttpRequestMessage(HttpMethod.Get, "https://example.com"),
            HttpCompletionOption.ResponseContentRead,
            (_, _) => Task.CompletedTask,
            CancellationToken.None);
}
