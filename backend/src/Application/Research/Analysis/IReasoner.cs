using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IReasoner
{
    Task<Result<ResearchResult>> ReasonAsync(
        ResearchContext context,
        CancellationToken cancellationToken);
}
