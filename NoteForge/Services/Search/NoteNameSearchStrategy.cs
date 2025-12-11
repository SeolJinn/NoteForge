using System;
using System.Collections.Generic;
using System.Linq;
using NoteForge.Models;

namespace NoteForge.Services.Search;

public class NoteNameSearchStrategy : ISearchStrategy<IEnumerable<Note>, Note>
{
    public IEnumerable<Note> Search(IEnumerable<Note> notes, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return notes;
        }

        var queryLower = query.Trim().ToLowerInvariant();

        var results = notes
            .Select(note => new
            {
                Note = note,
                Score = CalculateRelevanceScore(note.Filename, queryLower)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Note.Filename)
            .Select(x => x.Note);

        return results;
    }

    private static int CalculateRelevanceScore(string filename, string queryLower)
    {
        var filenameLower = filename.ToLowerInvariant();

        if (filenameLower == queryLower)
        {
            return 1000;
        }

        if (filenameLower.StartsWith(queryLower))
        {
            return 500;
        }

        if (filenameLower.Contains(queryLower))
        {
            return 100;
        }

        var queryWords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var matchedWords = queryWords.Count(word => filenameLower.Contains(word));

        if (matchedWords > 0)
        {
            return matchedWords * 10;
        }

        return 0;
    }
}