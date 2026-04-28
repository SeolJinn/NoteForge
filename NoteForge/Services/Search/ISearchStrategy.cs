using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoteForge.Services.Search;

public interface ISearchStrategy<TInput, TResult>
{
    IEnumerable<TResult> Search(TInput input, string query);

    Task<List<TResult>> SearchAsync(TInput input, string query)
    {
        return Task.FromResult<List<TResult>>([.. Search(input, query)]);
    }
}