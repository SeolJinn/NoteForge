using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NoteForge.Models;

namespace NoteForge.Services.Search;

public partial class SubstringSearchStrategy : ISearchStrategy<IEnumerable<Note>, SearchResult>
{
    [GeneratedRegex("\"([^\"]+)\"|\\S+")]
    private static partial Regex QueryTermRegex();

    public IEnumerable<SearchResult> Search(IEnumerable<Note> input, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var terms = ParseQuery(query);
        if (terms.Count is 0)
            return [];

        List<string> lowerTerms = [.. terms.Select(t => t.ToLowerInvariant())];
        List<SearchResult> results = [];

        foreach (var note in input)
        {
            var content = note.Text ?? string.Empty;
            var filename = Path.GetFileNameWithoutExtension(note.FilePath) ?? string.Empty;
            var searchableText = filename + "\n" + content;

            if (!AllTermsMatch(searchableText, lowerTerms))
                continue;

            var matchingLines = ExtractMatchingLines(content, lowerTerms, query);
            var matchesInTitle = AllTermsMatch(filename, lowerTerms);

            results.Add(new SearchResult(note, query)
            {
                MatchesInTitle = matchesInTitle,
                MatchingLines = matchingLines,
                RelevanceScore = 0f
            });
        }

        return results.OrderByDescending(r => r.Note.Date);
    }

    private static List<string> ParseQuery(string query)
    {
        List<string> terms = [];
        foreach (Match match in QueryTermRegex().Matches(query))
        {
            var term = match.Groups[1].Success ? match.Groups[1].Value : match.Value;
            if (!string.IsNullOrWhiteSpace(term))
                terms.Add(term);
        }

        return terms;
    }

    private static bool AllTermsMatch(string text, List<string> terms)
    {
        var textLower = text.ToLowerInvariant();
        return terms.All(term => textLower.Contains(term));
    }

    private static List<MatchingLine> ExtractMatchingLines(string content, List<string> terms, string query, int maxExcerpts = 5)
    {
        var lines = content.Split(['\r', '\n'], StringSplitOptions.None);
        List<int> matchingLineIndices = [];

        for (int i = 0; i < lines.Length; i++)
        {
            var lineLower = lines[i].ToLowerInvariant();
            if (terms.Any(term => lineLower.Contains(term)) && !string.IsNullOrWhiteSpace(lines[i]))
                matchingLineIndices.Add(i);
        }

        return [.. matchingLineIndices
            .Take(maxExcerpts)
            .Select(i => new MatchingLine(lines[i].Trim(), query, i + 1))];
    }
}
