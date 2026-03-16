using System.Collections.Generic;
using System.Threading.Tasks;
using NoteForge.Models;
using NoteForge.Services.Search;

namespace NoteForge.Interfaces;

public interface ISemanticSearchStrategy : ISearchStrategy<IEnumerable<Note>, SearchResult>
{
    void InvalidateIndex();
    void InvalidateEmbeddingsCache();
    new Task<List<SearchResult>> SearchAsync(IEnumerable<Note> notes, string query);
}
