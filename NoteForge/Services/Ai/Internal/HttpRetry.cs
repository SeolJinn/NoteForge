using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NoteForge.Services.Ai.Internal;

internal static class HttpRetry
{
    public static async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
        var response = await client.SendAsync(requestFactory(), completionOption, cancellationToken);

        if (!ShouldRetry(response))
        {
            return response;
        }

        var delay = ResolveRetryDelay(response);
        response.Dispose();

        await Task.Delay(delay, cancellationToken);

        return await client.SendAsync(requestFactory(), completionOption, cancellationToken);
    }

    private static bool ShouldRetry(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;
        return status is 429 || (status >= 500 && status < 600);
    }

    private static TimeSpan ResolveRetryDelay(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter is not null)
        {
            if (retryAfter.Delta is { } delta && delta > TimeSpan.Zero)
            {
                return Cap(delta);
            }
            if (retryAfter.Date is { } date)
            {
                var fromDate = date - DateTimeOffset.UtcNow;
                if (fromDate > TimeSpan.Zero)
                {
                    return Cap(fromDate);
                }
            }
        }
        return TimeSpan.FromSeconds(1);
    }

    private static TimeSpan Cap(TimeSpan delay)
    {
        var max = TimeSpan.FromSeconds(30);
        return delay > max ? max : delay;
    }
}
