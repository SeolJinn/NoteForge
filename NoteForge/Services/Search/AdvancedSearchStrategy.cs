using System;
using System.Collections.Generic;
using NoteForge.Models;

namespace NoteForge.Services.Search;

public class AdvancedSearchStrategy : ISearchStrategy<IEnumerable<Note>, List<SearchResult>>
{
    public IEnumerable<List<SearchResult>> Search(IEnumerable<Note> notes, string query)
    {
        var results = ParseAndSearch(notes, query);
        yield return results;
    }

    private List<SearchResult> ParseAndSearch(IEnumerable<Note> notes, string query)
    {
        var filters = new Dictionary<string, string>();
        var remainingQuery = query;

        var filterPrefixes = new[] { "file:", "path:" };

        foreach (var prefix in filterPrefixes)
        {
            var index = remainingQuery.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var start = index + prefix.Length;
                var end = remainingQuery.IndexOf(' ', start);
                var value = end >= 0
                    ? remainingQuery[start..end]
                    : remainingQuery[start..];

                filters[prefix.TrimEnd(':')] = value.Trim();
                remainingQuery = remainingQuery.Remove(index, (end >= 0 ? end : remainingQuery.Length) - index);
            }
        }

        remainingQuery = remainingQuery.Trim();

        var searchResults = new List<SearchResult>();

        foreach (var note in notes)
        {
            var matchesFile = !filters.TryGetValue("file", out var fileFilter) ||
                             note.Filename.Contains(fileFilter, StringComparison.OrdinalIgnoreCase);

            var matchesPath = !filters.TryGetValue("path", out var pathFilter) ||
                             note.FilePath.Contains(pathFilter, StringComparison.OrdinalIgnoreCase);

            if (!matchesFile || !matchesPath)
                continue;

            if (string.IsNullOrWhiteSpace(remainingQuery))
            {
                searchResults.Add(new SearchResult(note, remainingQuery) { MatchesInTitle = true });
                continue;
            }

            var titleMatches = note.Filename.Contains(remainingQuery, StringComparison.OrdinalIgnoreCase);
            var contentMatches = new List<MatchingLine>();

            if (!string.IsNullOrEmpty(note.Text))
            {
                var lines = note.Text.Split(['\r', '\n'], StringSplitOptions.None);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.IsNullOrEmpty(lines[i]))
                        continue;

                    if (lines[i].Contains(remainingQuery, StringComparison.OrdinalIgnoreCase))
                    {
                        var trimmedLine = lines[i].Trim();
                        if (!string.IsNullOrEmpty(trimmedLine))
                        {
                            contentMatches.Add(new MatchingLine(trimmedLine, remainingQuery, i + 1));
                        }
                    }
                }
            }

            if (titleMatches || contentMatches.Count > 0)
            {
                if (contentMatches.Count > 0)
                {
                    contentMatches[^1].IsLast = true;
                }

                searchResults.Add(new SearchResult(note, remainingQuery)
                {
                    MatchesInTitle = titleMatches,
                    MatchingLines = contentMatches
                });
            }
        }

        return searchResults;
    }
}