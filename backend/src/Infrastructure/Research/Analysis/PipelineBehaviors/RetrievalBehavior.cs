using System;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class RetrievalBehavior : IResearchPipelineBehavior
{
    private readonly IEvidenceRepository _repository;

    public RetrievalBehavior(IEvidenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        if (executionContext.CurrentStage == PipelineStage.Retrieval)
        {
            var corpus = await _repository.GetEvidenceAsync(executionContext.Context.Input.Query, cancellationToken);
            var updatedContext = executionContext.Context.WithCorpus(corpus);
            var updatedExecContext = executionContext
                .WithContext(updatedContext)
                .TransitionTo(PipelineStage.Deduplication);

            return await next(updatedExecContext);
        }

        return await next(executionContext);
    }
}
