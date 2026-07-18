using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IResearchPipeline
{
    Task<Result<ResearchExecutionContext>> ExecuteAsync(
        QueryAnalysis query, 
        System.Guid? sessionId = null,
        System.Guid? workspaceId = null,
        CancellationToken cancellationToken = default);
}

public interface IResearchPipelineBehavior
{
    Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        System.Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken);
}
