using System;
using System.Collections.Generic;

namespace NoteForge.Services.Search;

public class TextMatch
{
    public int Position { get; set; }
    public int Length { get; set; }
}

public class InFileTextSearchStrategy : ISearchStrategy<string, TextMatch>
{
    public IEnumerable<TextMatch> Search(string content, string query)
    {
        var matches = new List<TextMatch>();

        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(content))
        {
            return matches;
        }

        var index = 0;
        while ((index = content.IndexOf(query, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            matches.Add(new TextMatch
            {
                Position = index,
                Length = query.Length
            });
            index += query.Length;
        }

        return matches;
    }
}
