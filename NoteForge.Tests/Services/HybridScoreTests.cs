using NoteForge.Services.Embeddings;

namespace NoteForge.Tests.Services;

public class HybridScoreTests
{
    private const double SemanticFloor = 0.25;
    private const double LexicalWeight = 0.15;
    private const double FilenameBoost = 0.15;

    private static (bool keep, double score) Score(double semantic, double tfidf, bool filenameMatch = false) =>
        VectorMath.HybridScore(semantic, tfidf, filenameMatch, SemanticFloor, LexicalWeight, FilenameBoost);

    [Fact]
    public void Semantic_match_with_no_lexical_overlap_is_kept()
    {
        var (keep, score) = Score(semantic: 0.5, tfidf: 0.0);

        Assert.True(keep);
        Assert.Equal(0.5, score, 5);
    }

    [Fact]
    public void Below_semantic_floor_is_dropped_regardless_of_lexical_score()
    {
        var (keep, score) = Score(semantic: 0.2, tfidf: 1.0);

        Assert.False(keep);
        Assert.Equal(0, score);
    }

    [Fact]
    public void Filename_match_admits_note_below_semantic_floor()
    {
        var (keep, _) = Score(semantic: 0.1, tfidf: 0.0, filenameMatch: true);

        Assert.True(keep);
    }

    [Fact]
    public void Lexical_overlap_breaks_ties_at_equal_semantic()
    {
        var withLexical = Score(semantic: 0.3, tfidf: 1.0).score;
        var withoutLexical = Score(semantic: 0.3, tfidf: 0.0).score;

        Assert.True(withLexical > withoutLexical);
    }

    [Fact]
    public void Clearly_higher_semantic_outranks_max_lexical_on_a_weaker_note()
    {
        var strongSemantic = Score(semantic: 0.5, tfidf: 0.0).score;
        var weakSemanticMaxLexical = Score(semantic: 0.3, tfidf: 1.0).score;

        Assert.True(strongSemantic > weakSemanticMaxLexical);
    }

    [Fact]
    public void Higher_semantic_base_outranks_lower_base_at_equal_lexical()
    {
        var higher = Score(semantic: 0.6, tfidf: 0.3).score;
        var lower = Score(semantic: 0.4, tfidf: 0.3).score;

        Assert.True(higher > lower);
    }

    [Fact]
    public void Score_stays_within_unit_range()
    {
        var (_, score) = Score(semantic: 0.9, tfidf: 1.0, filenameMatch: true);

        Assert.InRange(score, 0.0, 1.0);
    }
}
