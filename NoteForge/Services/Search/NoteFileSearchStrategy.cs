using System;
using System.Collections.Generic;
using System.Linq;
using NoteForge.Models;

namespace NoteForge.Services.Search;

public class NoteFileSearchStrategy : ISearchStrategy<IEnumerable<Note>, Note>
{
    public IEnumerable<Note> Search(IEnumerable<Note> notes, string queryString)
    {
        var query = new SearchQuery(queryString);

        if (query.IsEmpty)
        {
            return notes;
        }

        var results = notes.AsEnumerable();

        if (query.Filters.TryGetValue("file", out var filename))
        {
            results = results.Where(n => n.Filename.Contains(filename, StringComparison.OrdinalIgnoreCase));
        }

        if (query.Filters.TryGetValue("path", out var path))
        {
            results = results.Where(n => n.FilePath.Contains(path, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.NormalizedQuery))
        {
            results = results.Where(n =>
                n.Filename.Contains(query.NormalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                n.FilePath.Contains(query.NormalizedQuery, StringComparison.OrdinalIgnoreCase));
        }

        return [.. results];
    }
}