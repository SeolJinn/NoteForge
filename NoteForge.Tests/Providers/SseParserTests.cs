using System.Text;
using NoteForge.Services.Ai.Internal;

namespace NoteForge.Tests.Providers;

public class SseParserTests
{
    [Fact]
    public async Task Parses_data_lines_and_terminates_on_DONE()
    {
        var raw = "data: {\"v\":1}\n\ndata: {\"v\":2}\n\ndata: [DONE]\n\n";
        var events = await ReadAllAsync(raw);
        Assert.Equal(["{\"v\":1}", "{\"v\":2}"], events);
    }

    [Fact]
    public async Task Skips_blank_lines_and_comments()
    {
        var raw = ": comment\n\ndata: {\"v\":1}\n\n: keepalive\ndata: [DONE]\n\n";
        var events = await ReadAllAsync(raw);
        Assert.Equal(["{\"v\":1}"], events);
    }

    [Fact]
    public async Task Handles_partial_chunks_across_reads()
    {
        var raw = "data: {\"hello\": \"world\"}\n\ndata: [DONE]\n\n";
        var events = await ReadAllAsync(raw);
        Assert.Single(events);
        Assert.Equal("{\"hello\": \"world\"}", events[0]);
    }

    private static async Task<List<string>> ReadAllAsync(string raw)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(raw));
        var events = new List<string>();
        await foreach (var ev in SseParser.ReadEventsAsync(stream))
        {
            events.Add(ev);
        }
        return events;
    }
}
