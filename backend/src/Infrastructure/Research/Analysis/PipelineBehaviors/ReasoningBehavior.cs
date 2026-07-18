using System;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class ReasoningBehavior : IResearchPipelineBehavior
{
    private readonly IReasoner _reasoner;

    public ReasoningBehavior(IReasoner reasoner)
    {
        _reasoner = reasoner;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        if (executionContext.CurrentStage == PipelineStage.Reasoning)
        {
            var startedAt = DateTimeOffset.UtcNow;
            
            var reasonResult = await _reasoner.ReasonAsync(executionContext.Context, cancellationToken);
            var finishedAt = DateTimeOffset.UtcNow;

            if (!reasonResult.IsSuccess)
            {
                return Result<ResearchExecutionContext>.Failure(reasonResult.Error!);
            }

            var researchResult = reasonResult.Value!;
            var stageExecution = new PipelineStageExecution(
                Stage: PipelineStage.Reasoning,
                StartedAt: startedAt,
                FinishedAt: finishedAt,
                Duration: finishedAt - startedAt
            );

            var updatedExecContext = executionContext
                .WithReasoning(researchResult.Session, researchResult.Reasoning)
                .WithValidation(researchResult.Validation)
                .WithExplainability(researchResult.Explainability)
                .WithStageExecution(stageExecution)
                .TransitionTo(PipelineStage.Validation);

            return await next(updatedExecContext);
        }

        return await next(executionContext);
    }
}
