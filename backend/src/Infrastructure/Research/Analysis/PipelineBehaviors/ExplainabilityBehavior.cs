using System;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class ExplainabilityBehavior : IResearchPipelineBehavior
{
    private readonly IExplainabilityBuilder _explainabilityBuilder;

    public ExplainabilityBehavior(IExplainabilityBuilder explainabilityBuilder)
    {
        _explainabilityBuilder = explainabilityBuilder;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        if (executionContext.CurrentStage == PipelineStage.Explainability)
        {
            if (executionContext.Reasoning == null)
            {
                return Result<ResearchExecutionContext>.Failure(new Error("MissingReasoning", "Reasoning result is missing during explainability stage.", ErrorSeverity.Error));
            }

            var startedAt = DateTimeOffset.UtcNow;
            var explainability = _explainabilityBuilder.BuildMap(executionContext.Reasoning, executionContext.Context);
            var finishedAt = DateTimeOffset.UtcNow;

            var stageExecution = new PipelineStageExecution(
                Stage: PipelineStage.Explainability,
                StartedAt: startedAt,
                FinishedAt: finishedAt,
                Duration: finishedAt - startedAt
            );

            var updatedExecContext = executionContext
                .WithExplainability(explainability)
                .WithStageExecution(stageExecution)
                .TransitionTo(PipelineStage.Rendering);

            return await next(updatedExecContext);
        }

        return await next(executionContext);
    }
}
