using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IResearchPipeline
{
    Task<Result<ResearchExecutionContext>> ExecuteAsync(
        QueryAnalysis query, 
        CancellationToken cancellationToken);
}

public interface IResearchPipelineBehavior
{
    Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        System.Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken);
}
