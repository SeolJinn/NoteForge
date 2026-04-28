using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NoteForge.Services.Ai.Internal;

public static class SseParser
{
    public static async IAsyncEnumerable<string> ReadEventsAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                yield break;
            }

            if (line.Length is 0 || line.StartsWith(':'))
            {
                continue;
            }

            if (!line.StartsWith("data:"))
            {
                continue;
            }

            var payload = line[5..].TrimStart();

            if (payload is "[DONE]")
            {
                yield break;
            }

            yield return payload;
        }
    }
}
