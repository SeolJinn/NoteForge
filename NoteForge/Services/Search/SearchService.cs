using System.Collections.Generic;
using NoteForge.Interfaces;
using NoteForge.Models;
using NoteForge.Services.Search;

namespace NoteForge.Services.Search;

public class SearchService : ISearchService
{
    private readonly ISearchStrategy<IEnumerable<Note>, Note> _nameSearchStrategy;

    public SearchService()
    {
        _nameSearchStrategy = new NoteNameSearchStrategy();
    }

    public IEnumerable<Note> SearchByName(IEnumerable<Note> notes, string query)
    {
        return _nameSearchStrategy.Search(notes, query);
    }
}