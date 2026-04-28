using NoteForge.Models;
using NoteForge.Services.Search;

namespace NoteForge.Tests.Services;

public class TfidfCalculatorTests
{
    private static Note MakeNote(string path, string text) => new()
    {
        FilePath = path,
        Filename = Path.GetFileNameWithoutExtension(path),
        Text = text
    };

    [Fact]
    public void Search_finds_exact_term_match()
    {
        var calc = new TfidfCalculator();
        var notes = new[]
        {
            MakeNote("/a.md", "machine learning concepts"),
            MakeNote("/b.md", "cooking recipes")
        };
        calc.BuildIndex(notes);

        var results = calc.Search(notes, "machine");

        Assert.Single(results, r => r.score > 0);
        Assert.Equal("/a.md", results.First(r => r.score > 0).note.FilePath);
    }

    [Fact]
    public void Search_with_stopword_only_query_returns_no_matches()
    {
        var calc = new TfidfCalculator();
        var notes = new[] { MakeNote("/a.md", "the and that of is") };
        calc.BuildIndex(notes);

        var results = calc.Search(notes, "the and");

        Assert.All(results, r => Assert.Equal(0, r.score));
    }

    [Fact]
    public void BuildIndex_filters_stopwords_from_documents()
    {
        var calc = new TfidfCalculator();
        var notes = new[] { MakeNote("/a.md", "the unique-term is here") };
        calc.BuildIndex(notes);

        var results = calc.Search(notes, "the");
        Assert.DoesNotContain(results, r => r.score > 0);

        var hits = calc.Search(notes, "unique");
        Assert.Contains(hits, r => r.score > 0);
    }

    [Fact]
    public void Search_accepts_two_char_terms_with_digits()
    {
        var calc = new TfidfCalculator();
        var notes = new[]
        {
            MakeNote("/a.md", "Q4 planning notes"),
            MakeNote("/b.md", "annual review")
        };
        calc.BuildIndex(notes);

        var results = calc.Search(notes, "q4");

        Assert.Single(results, r => r.score > 0);
        Assert.Equal("/a.md", results.First(r => r.score > 0).note.FilePath);
    }

    [Fact]
    public void Search_rejects_two_char_term_without_digit_or_all_letters()
    {
        var calc = new TfidfCalculator();
        var notes = new[] { MakeNote("/a.md", "x! marker") };
        calc.BuildIndex(notes);

        var results = calc.Search(notes, "x!");

        Assert.All(results, r => Assert.Equal(0, r.score));
    }

    [Fact]
    public void Search_accepts_two_char_all_letter_term()
    {
        var calc = new TfidfCalculator();
        var notes = new[]
        {
            MakeNote("/a.md", "Hz frequency measurements"),
            MakeNote("/b.md", "manual notes")
        };
        calc.BuildIndex(notes);

        var results = calc.Search(notes, "hz");

        Assert.Single(results, r => r.score > 0);
    }

    [Fact]
    public void Search_supports_prefix_matching_for_long_terms()
    {
        var calc = new TfidfCalculator();
        var notes = new[]
        {
            MakeNote("/a.md", "machine learning is fascinating"),
            MakeNote("/b.md", "cooking dinner")
        };
        calc.BuildIndex(notes);

        var results = calc.Search(notes, "machi");

        Assert.Single(results, r => r.score > 0);
        Assert.Equal("/a.md", results.First(r => r.score > 0).note.FilePath);
    }

    [Fact]
    public void Search_normalizes_scores_max_is_one()
    {
        var calc = new TfidfCalculator();
        var notes = new[]
        {
            MakeNote("/a.md", "machine machine machine learning"),
            MakeNote("/b.md", "machine learning")
        };
        calc.BuildIndex(notes);

        var results = calc.Search(notes, "machine");
        var positive = results.Where(r => r.score > 0).ToList();

        Assert.NotEmpty(positive);
        Assert.Equal(1.0, positive.Max(r => r.score), 5);
    }

    [Fact]
    public void Search_empty_query_returns_no_results()
    {
        var calc = new TfidfCalculator();
        var notes = new[] { MakeNote("/a.md", "anything") };
        calc.BuildIndex(notes);

        var results = calc.Search(notes, "");

        Assert.Empty(results);
    }

    [Fact]
    public void SparseCosineSimilarity_identical_vectors_is_one()
    {
        var v = new Dictionary<string, double> { ["a"] = 0.5, ["b"] = 0.7 };

        Assert.Equal(1.0, TfidfCalculator.SparseCosineSimilarity(v, v), 5);
    }

    [Fact]
    public void SparseCosineSimilarity_disjoint_vectors_is_zero()
    {
        var a = new Dictionary<string, double> { ["x"] = 1.0 };
        var b = new Dictionary<string, double> { ["y"] = 1.0 };

        Assert.Equal(0.0, TfidfCalculator.SparseCosineSimilarity(a, b));
    }

    [Fact]
    public void SparseCosineSimilarity_empty_input_returns_zero()
    {
        var nonEmpty = new Dictionary<string, double> { ["x"] = 1.0 };
        var empty = new Dictionary<string, double>();

        Assert.Equal(0.0, TfidfCalculator.SparseCosineSimilarity(empty, nonEmpty));
        Assert.Equal(0.0, TfidfCalculator.SparseCosineSimilarity(nonEmpty, empty));
    }

    [Fact]
    public void GetTfidfVectors_filters_zero_weight_terms()
    {
        var calc = new TfidfCalculator();
        var notes = new[]
        {
            MakeNote("/a.md", "common common common"),
            MakeNote("/b.md", "common common common"),
            MakeNote("/c.md", "rare unique")
        };
        calc.BuildIndex(notes);

        var vectors = calc.GetTfidfVectors();

        Assert.False(vectors["/a.md"].ContainsKey("common"));
        Assert.True(vectors["/c.md"].ContainsKey("rare"));
    }

    [Fact]
    public void Search_long_documents_score_lower_than_short_for_same_term()
    {
        var calc = new TfidfCalculator();
        var shortText = "uniqueterm";
        var longText = "uniqueterm " + string.Join(" ", Enumerable.Repeat("filler-content", 200));

        var notes = new[]
        {
            MakeNote("/short.md", shortText),
            MakeNote("/long.md", longText)
        };
        calc.BuildIndex(notes);

        var results = calc.Search(notes, "uniqueterm");
        var shortScore = results.First(r => r.note.FilePath == "/short.md").score;
        var longScore = results.First(r => r.note.FilePath == "/long.md").score;

        Assert.True(shortScore > longScore, $"Expected short ({shortScore}) > long ({longScore})");
    }
}
