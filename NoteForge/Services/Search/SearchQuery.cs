using System.Collections.Generic;

namespace NoteForge.Services.Search;

public class SearchQuery
{
    public string RawQuery { get; }
    public string NormalizedQuery { get; }
    public Dictionary<string, string> Filters { get; }

    public SearchQuery(string rawQuery)
    {
        RawQuery = rawQuery ?? string.Empty;
        Filters = new Dictionary<string, string>();
        NormalizedQuery = ParseQuery(rawQuery);
    }

    private string ParseQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        var remainingQuery = query;
        var filterPrefixes = new[] { "file:", "path:" };

        foreach (var prefix in filterPrefixes)
        {
            var index = remainingQuery.IndexOf(prefix, System.StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var start = index + prefix.Length;
                var end = remainingQuery.IndexOf(' ', start);
                var value = end >= 0
                    ? remainingQuery.Substring(start, end - start)
                    : remainingQuery.Substring(start);

                Filters[prefix.TrimEnd(':')] = value.Trim();
                remainingQuery = remainingQuery.Remove(index, (end >= 0 ? end : remainingQuery.Length) - index);
            }
        }

        return remainingQuery.Trim();
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(RawQuery);
    public bool HasFilters => Filters.Count > 0;
}
