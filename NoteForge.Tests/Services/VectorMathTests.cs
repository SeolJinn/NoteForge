using NoteForge.Services.Embeddings;

namespace NoteForge.Tests.Services;

public class VectorMathTests
{
    [Fact]
    public void CosineSimilarity_identical_vectors_is_one()
    {
        var v = new[] { 1f, 2f, 3f };
        Assert.Equal(1f, VectorMath.CosineSimilarity(v, v), 5);
    }

    [Fact]
    public void CosineSimilarity_orthogonal_vectors_is_zero()
    {
        var a = new[] { 1f, 0f, 0f };
        var b = new[] { 0f, 1f, 0f };
        Assert.Equal(0f, VectorMath.CosineSimilarity(a, b), 5);
    }

    [Fact]
    public void CosineSimilarity_opposite_vectors_is_negative_one()
    {
        var a = new[] { 1f, 0f, 0f };
        var b = new[] { -1f, 0f, 0f };
        Assert.Equal(-1f, VectorMath.CosineSimilarity(a, b), 5);
    }

    [Fact]
    public void CosineSimilarity_zero_vector_returns_zero()
    {
        var a = new[] { 1f, 2f, 3f };
        var z = new[] { 0f, 0f, 0f };
        Assert.Equal(0f, VectorMath.CosineSimilarity(a, z));
        Assert.Equal(0f, VectorMath.CosineSimilarity(z, a));
    }

    [Fact]
    public void CosineSimilarity_length_mismatch_throws()
    {
        var a = new[] { 1f, 2f, 3f };
        var b = new[] { 1f, 2f };
        Assert.Throws<ArgumentException>(() => VectorMath.CosineSimilarity(a, b));
    }

    [Fact]
    public void Normalize_unit_vector_returns_same()
    {
        var v = new[] { 1f, 0f, 0f };
        var n = VectorMath.Normalize(v);
        Assert.Equal(1f, n[0], 5);
        Assert.Equal(0f, n[1], 5);
    }

    [Fact]
    public void Normalize_scales_to_unit_length()
    {
        var v = new[] { 3f, 4f };
        var n = VectorMath.Normalize(v);
        var magnitude = MathF.Sqrt(n[0] * n[0] + n[1] * n[1]);
        Assert.Equal(1f, magnitude, 5);
    }

    [Fact]
    public void Normalize_zero_vector_returns_unchanged()
    {
        var z = new[] { 0f, 0f };
        var n = VectorMath.Normalize(z);
        Assert.Equal(0f, n[0]);
        Assert.Equal(0f, n[1]);
    }

    [Fact]
    public void HarmonicMean_two_equal_values_is_value()
    {
        Assert.Equal(0.5, VectorMath.HarmonicMean(0.5, 0.5), 5);
    }

    [Fact]
    public void HarmonicMean_with_zero_returns_zero()
    {
        Assert.Equal(0, VectorMath.HarmonicMean(0, 0.8));
        Assert.Equal(0, VectorMath.HarmonicMean(0.8, 0));
        Assert.Equal(0, VectorMath.HarmonicMean(0, 0));
    }

    [Fact]
    public void HarmonicMean_penalizes_imbalance_more_than_arithmetic()
    {
        var harmonic = VectorMath.HarmonicMean(0.1, 0.9);
        var arithmetic = (0.1 + 0.9) / 2;
        Assert.True(harmonic < arithmetic);
    }

    [Fact]
    public void RankBySimilarity_filters_below_threshold_and_orders_descending()
    {
        var query = new[] { 1f, 0f };
        var candidates = new[]
        {
            ("low", new[] { 0.1f, 1f }),
            ("high", new[] { 0.95f, 0.05f }),
            ("medium", new[] { 0.7f, 0.3f })
        };

        var results = VectorMath.RankBySimilarity<string>(query, candidates, minThreshold: 0.5f);

        Assert.Equal(2, results.Count);
        Assert.Equal("high", results[0].item);
        Assert.Equal("medium", results[1].item);
    }

    [Fact]
    public void RankBySimilarity_caps_at_maxResults()
    {
        var query = new[] { 1f, 0f };
        var candidates = Enumerable.Range(0, 10).Select(i => (i, new[] { 1f, 0f })).ToArray();

        var results = VectorMath.RankBySimilarity<int>(query, candidates, minThreshold: 0f, maxResults: 3);

        Assert.Equal(3, results.Count);
    }
}
