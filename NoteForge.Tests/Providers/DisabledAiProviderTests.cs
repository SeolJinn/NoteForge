using NoteForge.Services.Ai;

namespace NoteForge.Tests.Providers;

public class DisabledAiProviderTests
{
    [Fact]
    public async Task StreamCompletionAsync_throws_AiDisabledException()
    {
        var provider = new DisabledAiProvider();
        await Assert.ThrowsAsync<AiDisabledException>(async () =>
        {
            await foreach (var _ in provider.StreamCompletionAsync("hi"))
            {
            }
        });
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_throws_AiDisabledException()
    {
        var provider = new DisabledAiProvider();
        await Assert.ThrowsAsync<AiDisabledException>(() => provider.GenerateEmbeddingAsync("hi"));
    }

    [Fact]
    public async Task TestConnectionAsync_returns_failure_with_message()
    {
        var provider = new DisabledAiProvider();
        var result = await provider.TestConnectionAsync();
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void EmbeddingDimension_is_zero()
    {
        var provider = new DisabledAiProvider();
        Assert.Equal(0, provider.EmbeddingDimension);
    }
}
