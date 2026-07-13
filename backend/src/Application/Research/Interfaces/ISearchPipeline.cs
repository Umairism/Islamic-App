using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface ISearchPipeline
{
    Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken);
}
