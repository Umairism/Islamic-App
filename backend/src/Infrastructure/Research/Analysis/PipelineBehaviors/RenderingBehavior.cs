using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class RenderingBehavior : IResearchPipelineBehavior
{
    private readonly IEnumerable<IResearchRenderer> _renderers;

    public RenderingBehavior(IEnumerable<IResearchRenderer> renderers)
    {
        _renderers = renderers;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        if (executionContext.CurrentStage == PipelineStage.Rendering)
        {
            if (executionContext.Reasoning == null || executionContext.Validation == null || executionContext.Explainability == null)
            {
                return Result<ResearchExecutionContext>.Failure(new Error("MissingContextState", "Context state details are incomplete during rendering stage.", ErrorSeverity.Error));
            }

            var startedAt = DateTimeOffset.UtcNow;

            // Reconstruct the finalized envelope so renderers have full access to details
            var researchResult = new ResearchResult(
                ExecutionContext: executionContext,
                Session: executionContext.Session!,
                Reasoning: executionContext.Reasoning,
                Validation: executionContext.Validation,
                Explainability: executionContext.Explainability,
                Outputs: new List<RenderResult>()
            );

            var outputs = new List<RenderResult>();
            foreach (var renderer in _renderers)
            {
                var renderOutput = await renderer.RenderAsync(researchResult, cancellationToken);
                outputs.Add(renderOutput);
            }

            var finishedAt = DateTimeOffset.UtcNow;

            var stageExecution = new PipelineStageExecution(
                Stage: PipelineStage.Rendering,
                StartedAt: startedAt,
                FinishedAt: finishedAt,
                Duration: finishedAt - startedAt
            );

            var updatedExecContext = executionContext
                .WithRenderedOutputs(outputs.ToImmutableList())
                .WithStageExecution(stageExecution)
                .TransitionTo(PipelineStage.DossierGeneration);

            return await next(updatedExecContext);
        }

        return await next(executionContext);
    }
}
