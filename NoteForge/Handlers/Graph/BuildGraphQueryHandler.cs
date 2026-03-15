using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using NoteForge.Models;
using NoteForge.Services.Embeddings;
using NoteForge.Services.Search;

namespace NoteForge.Handlers.Graph;

public record BuildGraphQueryRequest(
    List<Note> Notes,
    GraphSettings Settings
) : IRequest<GraphData>;

public partial class BuildGraphQueryHandler(
    ILogger<BuildGraphQueryHandler> logger,
    TfidfCalculator tfidfCalculator) : IRequestHandler<BuildGraphQueryRequest, GraphData>
{
    private readonly ILogger<BuildGraphQueryHandler> _logger = logger;
    private readonly TfidfCalculator _tfidfCalculator = tfidfCalculator;

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled)]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled)]
    private static partial Regex WikiLinkRegex();

    public async ValueTask<GraphData> Handle(BuildGraphQueryRequest request, CancellationToken cancellationToken)
    {
        var graphData = new GraphData();
        var notes = request.Notes;
        var settings = request.Settings;

        var nodeMap = new Dictionary<string, GraphNode>();
        foreach (var note in notes)
        {
            var node = new GraphNode { Note = note };
            graphData.Nodes.Add(node);
            nodeMap[note.FilePath] = node;
        }

        _logger.LogInformation("Building graph with {NodeCount} nodes", graphData.Nodes.Count);

        var explicitEdges = settings.ShowExplicitLinks
            ? await BuildExplicitEdgesAsync(notes, nodeMap)
            : [];

        var semanticEdges = settings.ShowSemanticLinks
            ? await BuildSemanticEdgesAsync(notes, nodeMap, settings, explicitEdges)
            : [];

        foreach (var edge in explicitEdges.Concat(semanticEdges))
        {
            graphData.Edges.Add(edge);
        }

        _logger.LogInformation("Built graph with {EdgeCount} edges", graphData.Edges.Count);

        return graphData;
    }

    private async Task<List<GraphEdge>> BuildExplicitEdgesAsync(
        List<Note> notes,
        Dictionary<string, GraphNode> nodeMap)
    {
        var edges = new List<GraphEdge>();

        foreach (var sourceNote in notes)
        {
            var links = ExtractMarkdownLinks(sourceNote.Text, sourceNote.FilePath);

            foreach (var targetPath in links)
            {
                if (nodeMap.TryGetValue(targetPath, out var targetNode) &&
                    nodeMap.TryGetValue(sourceNote.FilePath, out var sourceNode))
                {
                    edges.Add(new GraphEdge
                    {
                        Source = sourceNode,
                        Target = targetNode,
                        Type = EdgeType.Explicit,
                        Strength = 1.0f
                    });
                }
            }
        }

        _logger.LogDebug("Found {Count} explicit links", edges.Count);
        return await Task.FromResult(edges);
    }

    private async Task<List<GraphEdge>> BuildSemanticEdgesAsync(
        List<Note> notes,
        Dictionary<string, GraphNode> nodeMap,
        GraphSettings settings,
        List<GraphEdge> explicitEdges)
    {
        if (App.EmbeddingRepository is null)
        {
            _logger.LogWarning("EmbeddingRepository not available for semantic edges");
            return [];
        }

        var edges = new List<GraphEdge>();
        var allEmbeddings = await App.EmbeddingRepository.GetAllEmbeddingsAsync();
        var embeddingMap = allEmbeddings.ToDictionary(e => e.FilePath, e => e.Embedding);

        _tfidfCalculator.BuildIndex(notes);

        var explicitPairs = new HashSet<string>(
            explicitEdges.Select(e => GetEdgeKey(e.Source.FilePath, e.Target.FilePath))
        );

        for (int i = 0; i < notes.Count; i++)
        {
            var sourceNote = notes[i];
            if (!embeddingMap.TryGetValue(sourceNote.FilePath, out var sourceEmbedding))
                continue;

            for (int j = i + 1; j < notes.Count; j++)
            {
                var targetNote = notes[j];
                if (!embeddingMap.TryGetValue(targetNote.FilePath, out var targetEmbedding))
                    continue;

                var edgeKey = GetEdgeKey(sourceNote.FilePath, targetNote.FilePath);
                var hasExplicitLink = explicitPairs.Contains(edgeKey);

                var semanticScore = VectorMath.CosineSimilarity(sourceEmbedding, targetEmbedding);

                var tfidfScore = CalculateTfidfSimilarity(sourceNote, targetNote);

                if (semanticScore < settings.SemanticThreshold || tfidfScore < settings.TfidfThreshold)
                    continue;

                var harmonicMean = 2.0 * (semanticScore * tfidfScore) / (semanticScore + tfidfScore);

                if (harmonicMean < 0.25)
                    continue;

                var edgeType = hasExplicitLink ? EdgeType.Hybrid : EdgeType.Semantic;

                edges.Add(new GraphEdge
                {
                    Source = nodeMap[sourceNote.FilePath],
                    Target = nodeMap[targetNote.FilePath],
                    Type = edgeType,
                    Strength = (float)harmonicMean
                });
            }
        }

        _logger.LogDebug("Found {Count} semantic edges", edges.Count);
        return edges;
    }

    private double CalculateTfidfSimilarity(Note note1, Note note2)
    {
        var results1 = _tfidfCalculator.Search([note2], note1.Text);
        var score1 = results1.FirstOrDefault(r => r.note.FilePath == note2.FilePath).score;

        var results2 = _tfidfCalculator.Search([note1], note2.Text);
        var score2 = results2.FirstOrDefault(r => r.note.FilePath == note1.FilePath).score;

        return Math.Max(score1, score2);
    }

    private List<string> ExtractMarkdownLinks(string text, string sourceFilePath)
    {
        var links = new List<string>();
        var sourceDir = Path.GetDirectoryName(sourceFilePath) ?? "";

        var markdownMatches = MarkdownLinkRegex().Matches(text);
        foreach (Match match in markdownMatches)
        {
            var linkPath = match.Groups[2].Value;
            var resolvedPath = ResolveLinkPath(linkPath, sourceDir);
            if (resolvedPath is not null)
                links.Add(resolvedPath);
        }

        var wikiMatches = WikiLinkRegex().Matches(text);
        foreach (Match match in wikiMatches)
        {
            var linkName = match.Groups[1].Value;
            var resolvedPath = ResolveWikiLink(linkName, sourceDir);
            if (resolvedPath is not null)
                links.Add(resolvedPath);
        }

        return links;
    }

    private string? ResolveLinkPath(string linkPath, string sourceDir)
    {
        if (linkPath.StartsWith("http://") || linkPath.StartsWith("https://"))
            return null;

        if (!linkPath.EndsWith(".md"))
            linkPath += ".md";

        var absolutePath = Path.IsPathRooted(linkPath)
            ? linkPath
            : Path.GetFullPath(Path.Combine(sourceDir, linkPath));

        return File.Exists(absolutePath) ? absolutePath : null;
    }

    private string? ResolveWikiLink(string linkName, string sourceDir)
    {
        var vaultRoot = App.NoteService.CurrentNotebookPath;
        if (string.IsNullOrEmpty(vaultRoot))
            return null;

        var fileName = linkName.EndsWith(".md") ? linkName : $"{linkName}.md";

        var searchPath = Path.Combine(vaultRoot, fileName);
        if (File.Exists(searchPath))
            return searchPath;

        var allMdFiles = Directory.GetFiles(vaultRoot, fileName, SearchOption.AllDirectories);
        return allMdFiles.Length == 1 ? allMdFiles[0] : null;
    }

    private static string GetEdgeKey(string path1, string path2)
    {
        return string.Compare(path1, path2, StringComparison.Ordinal) < 0
            ? $"{path1}|{path2}"
            : $"{path2}|{path1}";
    }
}
