using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using NoteForge.Handlers.Notes;
using NoteForge.Interfaces;
using NoteForge.Models;
using NoteForge.Services.Embeddings;

namespace NoteForge.Services.Search;

public class SearchEvaluationHarness(
    IMediator mediator,
    IAiService aiService,
    IEmbeddingRepository embeddingRepository,
    ISemanticSearchStrategy semanticSearch,
    INoteService noteService,
    ILogger<SearchEvaluationHarness> logger)
{
    private const string JudgmentsFileName = "search-eval.json";
    private const string ResultsFileName = "search-eval-results.txt";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<string> RunAsync()
    {
        if (!noteService.IsConfigured)
            return "No vault is loaded.";

        var vaultPath = noteService.CurrentNotebookPath;
        var judgmentsPath = Path.Combine(vaultPath, JudgmentsFileName);
        if (!File.Exists(judgmentsPath))
            return $"No judgments file found. Create {judgmentsPath}.";

        List<JudgmentEntry> judgments;
        try
        {
            var json = await File.ReadAllTextAsync(judgmentsPath);
            judgments = JsonSerializer.Deserialize<List<JudgmentEntry>>(json, JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse judgments file");
            return $"Failed to parse {JudgmentsFileName}: {ex.Message}";
        }

        var notes = (await mediator.Send(new GetNotesQueryRequest())).ToList();
        if (notes.Count is 0)
            return "Vault has no notes.";

        var lexical = new TfidfCalculator();
        lexical.BuildIndex(notes);
        semanticSearch.InvalidateIndex();
        semanticSearch.InvalidateEmbeddingsCache();

        var embeddings = (await embeddingRepository.GetAllEmbeddingsAsync())
            .ToDictionary(e => e.FilePath, e => e.Embedding);

        List<QueryResult> results = [];
        var perQuery = new StringBuilder();

        foreach (var entry in judgments)
        {
            if (string.IsNullOrWhiteSpace(entry.Query) || entry.Relevant is null)
                continue;

            var relevant = entry.Relevant.Select(Normalize).Where(s => s.Length > 0).ToHashSet();
            if (relevant.Count is 0)
                continue;

            var lex = Score(RankLexical(lexical, notes, entry.Query), relevant);
            var sem = Score(await RankSemanticAsync(notes, embeddings, entry.Query), relevant);
            var hyb = Score(await RankHybridAsync(notes, entry.Query), relevant);

            var type = string.IsNullOrWhiteSpace(entry.Type) ? "Other" : CapitalizeLabel(entry.Type.Trim());
            results.Add(new QueryResult(type, lex, sem, hyb));

            perQuery.AppendLine(
                $"[{type}] \"{entry.Query}\" (relevant={relevant.Count})  " +
                $"R-prec lex/sem/hyb = {lex.RPrecision:F2}/{sem.RPrecision:F2}/{hyb.RPrecision:F2}  " +
                $"nDCG@10 = {lex.NdcgAt10:F2}/{sem.NdcgAt10:F2}/{hyb.NdcgAt10:F2}  " +
                $"MRR = {lex.ReciprocalRank:F2}/{sem.ReciprocalRank:F2}/{hyb.ReciprocalRank:F2}");
        }

        if (results.Count is 0)
            return "No usable judgments. Each entry needs a non-empty query and at least one relevant file.";

        var report = BuildReport(notes.Count, results, perQuery.ToString());
        var resultsPath = Path.Combine(vaultPath, ResultsFileName);
        await File.WriteAllTextAsync(resultsPath, report);
        logger.LogInformation("Search evaluation written to {Path}\n{Report}", resultsPath, report);
        return $"Evaluated {results.Count} queries over {notes.Count} notes.\nResults written to {resultsPath}";
    }

    private static List<string> RankLexical(TfidfCalculator lexical, List<Note> notes, string query) =>
        [.. lexical.Search(notes, query)
            .OrderByDescending(r => r.score)
            .Select(r => Normalize(Path.GetFileNameWithoutExtension(r.note.FilePath)))];

    private async Task<List<string>> RankSemanticAsync(
        List<Note> notes,
        Dictionary<string, float[]> embeddings,
        string query)
    {
        var queryEmbedding = await aiService.GenerateEmbeddingAsync(query);
        if (queryEmbedding is null)
            return [];

        var candidates = notes
            .Where(n => embeddings.TryGetValue(n.FilePath, out var e) && e.Length == queryEmbedding.Length)
            .Select(n => (item: n, embedding: embeddings[n.FilePath]));

        return [.. VectorMath.RankBySimilarity(queryEmbedding, candidates, minThreshold: -1f, maxResults: notes.Count)
            .Select(r => Normalize(Path.GetFileNameWithoutExtension(r.item.FilePath)))];
    }

    private async Task<List<string>> RankHybridAsync(List<Note> notes, string query)
    {
        var results = await semanticSearch.SearchAsync(notes, query);
        return [.. results.Select(r => Normalize(Path.GetFileNameWithoutExtension(r.Note.FilePath)))];
    }

    private static QueryMetrics Score(List<string> ranked, HashSet<string> relevant)
    {
        var relevantCount = relevant.Count;

        var rPrecision = ranked.Take(relevantCount).Count(relevant.Contains) / (double)relevantCount;
        var recallAt10 = ranked.Take(10).Count(relevant.Contains) / (double)relevantCount;

        var dcg = 0.0;
        for (var i = 0; i < Math.Min(10, ranked.Count); i++)
        {
            if (relevant.Contains(ranked[i]))
                dcg += 1.0 / Math.Log2(i + 2);
        }

        var idcg = 0.0;
        for (var i = 0; i < Math.Min(10, relevantCount); i++)
            idcg += 1.0 / Math.Log2(i + 2);

        var ndcgAt10 = idcg is 0 ? 0 : dcg / idcg;

        var reciprocalRank = 0.0;
        for (var i = 0; i < ranked.Count; i++)
        {
            if (relevant.Contains(ranked[i]))
            {
                reciprocalRank = 1.0 / (i + 1);
                break;
            }
        }

        return new QueryMetrics(rPrecision, recallAt10, ndcgAt10, reciprocalRank);
    }

    private static string Normalize(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[..^3];
        return trimmed.Trim().ToLowerInvariant();
    }

    private static string BuildReport(int noteCount, List<QueryResult> results, string perQuery)
    {
        List<string> types = [];
        foreach (var r in results)
            if (!types.Contains(r.Type))
                types.Add(r.Type);

        var sb = new StringBuilder();
        sb.AppendLine("NoteForge search evaluation");
        sb.AppendLine($"Notes: {noteCount}   Queries evaluated: {results.Count}");
        sb.AppendLine("Metrics averaged over queries: R-Precision, Recall@10, nDCG@10, MRR");
        sb.AppendLine();

        void AppendTextRow(string name, QueryMetrics m) =>
            sb.AppendLine($"  {name,-18}  {m.RPrecision,7:F3}  {m.RecallAt10,6:F3}  {m.NdcgAt10,8:F3}  {m.ReciprocalRank,6:F3}");

        void AppendTextGroup(string label, List<QueryResult> group)
        {
            sb.AppendLine($"{label} ({group.Count} queries)");
            sb.AppendLine($"  {"Strategy",-18}  {"R-Prec",7}  {"R@10",6}  {"nDCG@10",8}  {"MRR",6}");
            AppendTextRow("Lexical (TF-IDF)", Mean(group, r => r.Lex));
            AppendTextRow("Semantic only", Mean(group, r => r.Sem));
            AppendTextRow("Hybrid", Mean(group, r => r.Hyb));
            sb.AppendLine();
        }

        foreach (var type in types)
            AppendTextGroup($"{type} queries", [.. results.Where(r => r.Type == type)]);
        AppendTextGroup("All", results);

        sb.AppendLine("Per query:");
        sb.Append(perQuery);
        sb.AppendLine();

        void AppendLatexRow(string name, QueryMetrics m) =>
            sb.AppendLine($"{name} & {m.RPrecision:F3} & {m.RecallAt10:F3} & {m.NdcgAt10:F3} & {m.ReciprocalRank:F3} \\\\");

        void AppendLatexGroup(string label, List<QueryResult> group)
        {
            sb.AppendLine($"\\multicolumn{{5}}{{l}}{{\\textit{{{label} ({group.Count} queries)}}}} \\\\");
            AppendLatexRow("Lexical (TF-IDF)", Mean(group, r => r.Lex));
            AppendLatexRow("Semantic only", Mean(group, r => r.Sem));
            AppendLatexRow("Hybrid", Mean(group, r => r.Hyb));
            sb.AppendLine("\\hline");
        }

        sb.AppendLine("LaTeX:");
        sb.AppendLine("\\begin{table}[H]");
        sb.AppendLine("\\centering");
        sb.AppendLine("\\begin{tabular}{lcccc}");
        sb.AppendLine("\\hline");
        sb.AppendLine("Strategy & R-Precision & Recall@10 & nDCG@10 & MRR \\\\");
        sb.AppendLine("\\hline");
        foreach (var type in types)
            AppendLatexGroup($"{type} queries", [.. results.Where(r => r.Type == type)]);
        AppendLatexGroup("All queries", results);
        sb.AppendLine("\\end{tabular}");
        sb.AppendLine($"\\caption{{Retrieval quality of the three search strategies over a {noteCount}-note vault and {results.Count} hand-labelled queries, split by query type. R-Precision is precision at a cut-off equal to the number of relevant notes for each query; Recall@10 is over the top 10 results; nDCG@10 is the normalised discounted cumulative gain over the top 10; MRR is the mean reciprocal rank of the first relevant note.}}");
        sb.AppendLine("\\label{tab:search-eval}");
        sb.AppendLine("\\end{table}");
        return sb.ToString();
    }

    private static QueryMetrics Mean(IReadOnlyCollection<QueryResult> group, Func<QueryResult, QueryMetrics> selector)
    {
        if (group.Count is 0)
            return new QueryMetrics(0, 0, 0, 0);

        return new QueryMetrics(
            group.Average(r => selector(r).RPrecision),
            group.Average(r => selector(r).RecallAt10),
            group.Average(r => selector(r).NdcgAt10),
            group.Average(r => selector(r).ReciprocalRank));
    }

    private static string CapitalizeLabel(string value) =>
        value.Length is 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];

    private sealed record JudgmentEntry(string Query, List<string> Relevant, string? Type);

    private readonly record struct QueryMetrics(double RPrecision, double RecallAt10, double NdcgAt10, double ReciprocalRank);

    private sealed record QueryResult(string Type, QueryMetrics Lex, QueryMetrics Sem, QueryMetrics Hyb);
}
