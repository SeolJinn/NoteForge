using System.Collections.Generic;

namespace NoteForge.Services.Search;

public interface ISearchStrategy<TInput, TResult>
{
    IEnumerable<TResult> Search(TInput input, string query);
}