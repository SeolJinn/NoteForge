using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NoteForge.Interfaces;
using NoteForge.Models;
using NoteForge.Services.Embeddings;

namespace NoteForge.Services.Search;

public class SemanticSearchStrategy(
    IOllamaService ollamaService,
    IEmbeddingRepository embeddingRepository,
    ILogger<SemanticSearchStrategy> logger) : ISemanticSearchStrategy
{
    private readonly IOllamaService _ollamaService = ollamaService;
    private readonly ILogger<SemanticSearchStrategy> _logger = logger;
    private readonly TfidfCalculator _tfidfCalculator = new();
    private bool _tfidfIndexDirty = true;
    private Dictionary<string, float[]>? _cachedEmbeddings;
    private bool _embeddingsCacheDirty = true;

    public void InvalidateIndex()
    {
        _tfidfIndexDirty = true;
    }

    public void InvalidateEmbeddingsCache()
    {
        _embeddingsCacheDirty = true;
    }

    public IEnumerable<SearchResult> Search(IEnumerable<Note> notes, string query)
    {
        return SearchCoreAsync(notes, query).GetAwaiter().GetResult();
    }

    public async Task<List<SearchResult>> SearchAsync(IEnumerable<Note> notes, string query)
    {
        return await SearchCoreAsync(notes, query);
    }

    private async Task<List<SearchResult>> SearchCoreAsync(IEnumerable<Note> notes, string query)
    {
        try
        {
            if (!embeddingRepository.IsInitialized)
            {
                _logger.LogWarning("EmbeddingRepository not initialized - vault may not be loaded");
                return [];
            }

            List<Note> notesList = [.. notes];

            if (_tfidfIndexDirty)
            {
                _tfidfCalculator.BuildIndex(notesList);
                _tfidfIndexDirty = false;
                _logger.LogDebug("Rebuilt TF-IDF index for {Count} notes", notesList.Count);
            }

            var queryEmbedding = await _ollamaService.GenerateEmbeddingAsync(query);
            if (queryEmbedding is null)
            {
                _logger.LogWarning("Failed to generate embedding for query: {Query}", query);
                return [];
            }

            if (_embeddingsCacheDirty || _cachedEmbeddings is null)
            {
                var allEmbeddings = await embeddingRepository.GetAllEmbeddingsAsync();
                _cachedEmbeddings = allEmbeddings.ToDictionary(e => e.FilePath, e => e.Embedding);
                _embeddingsCacheDirty = false;
                _logger.LogDebug("Loaded {Count} embeddings from database", _cachedEmbeddings.Count);
            }

            var embeddingMap = _cachedEmbeddings;

            var candidates = notesList
                .Where(n => embeddingMap.ContainsKey(n.FilePath))
                .Select(n => (note: n, embedding: embeddingMap[n.FilePath]));

            var semanticResults = VectorMath.RankBySimilarity(queryEmbedding, candidates, minThreshold: 0.0f);
            var tfidfResults = _tfidfCalculator.Search(notesList, query);

            var semanticScores = semanticResults.ToDictionary(r => r.item.FilePath, r => (double)r.score);
            var tfidfScores = tfidfResults.ToDictionary(r => r.note.FilePath, r => r.score);

            List<(Note note, float hybridScore, float semanticScore, float tfidfScore)> hybridResults = [];

            foreach (var note in notesList.Where(n => embeddingMap.ContainsKey(n.FilePath)))
            {
                var semanticScore = semanticScores.GetValueOrDefault(note.FilePath, 0.0);
                var tfidfScore = tfidfScores.GetValueOrDefault(note.FilePath, 0.0);

                if (tfidfScore < 0.15 || semanticScore < 0.15)
                    continue;

                var hybridScore = (float)VectorMath.HarmonicMean(semanticScore, tfidfScore);

                var filenameBoost = 0f;
                var queryLower = query.ToLowerInvariant();
                var filenameLower = Path.GetFileNameWithoutExtension(note.FilePath).ToLowerInvariant();
                if (filenameLower.Contains(queryLower))
                {
                    filenameBoost = 0.15f;
                    hybridScore += filenameBoost;
                }

                if (hybridScore > 0.25f)
                {
                    hybridResults.Add((note, hybridScore, (float)semanticScore, (float)tfidfScore));
                }
            }

            List<(Note note, float hybridScore, float semanticScore, float tfidfScore)> rankedResults =
                [.. hybridResults.OrderByDescending(r => r.hybridScore).Take(50)];

            _logger.LogInformation("Hybrid search for '{Query}' returned {Count} results", query, rankedResults.Count);
            foreach (var (note, hybridScore, semanticScore, tfidfScore) in rankedResults.Take(5))
            {
                var contentPreview = note.Text.Length > 50
                    ? note.Text[..50].Replace("\n", " ").Replace("\r", "") + "..."
                    : note.Text.Replace("\n", " ").Replace("\r", "");

                _logger.LogInformation("  {Score:F2}% - {File} (len: {Len}) [Semantic: {Sem:F2}%, TF-IDF: {Tfidf:F2}%]",
                    hybridScore * 100,
                    Path.GetFileName(note.FilePath),
                    note.Text.Length,
                    semanticScore * 100,
                    tfidfScore * 100);
                _logger.LogDebug("    Content: {Content}", contentPreview);
            }

            return [.. rankedResults.Select(r => CreateSearchResult(r.note, r.hybridScore, query))];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during hybrid search for query: {Query}", query);
            return [];
        }
    }

    private SearchResult CreateSearchResult(Note note, float relevanceScore, string query)
    {
        var excerpts = ExtractRelevantExcerpts(note.Text, query);

        return new SearchResult(note, query)
        {
            MatchesInTitle = false,
            MatchingLines = excerpts,
            RelevanceScore = relevanceScore
        };
    }

    private static List<MatchingLine> ExtractRelevantExcerpts(string text, string query, int maxExcerpts = 3)
    {
        var queryWords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = text.Split(['\r', '\n'], StringSplitOptions.None);
        List<(int lineNum, int matchCount, string line)> matches = [];

        for (int i = 0; i < lines.Length; i++)
        {
            var lineLower = lines[i].ToLower();
            int matchCount = queryWords.Count(word => lineLower.Contains(word));
            if (matchCount > 0 && !string.IsNullOrWhiteSpace(lines[i]))
            {
                matches.Add((i + 1, matchCount, lines[i].Trim()));
            }
        }

        return [.. matches
            .OrderByDescending(m => m.matchCount)
            .Take(maxExcerpts)
            .OrderBy(m => m.lineNum)
            .Select(m => new MatchingLine(m.line, query, m.lineNum))];
    }
}
