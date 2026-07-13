using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface ISearchPipelineStage
{
    Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken);
}
