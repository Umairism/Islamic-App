using System;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class ValidationBehavior : IResearchPipelineBehavior
{
    private readonly IResearchValidator _validator;
    private readonly IReasoningTelemetry _telemetry;

    public ValidationBehavior(IResearchValidator validator, IReasoningTelemetry telemetry)
    {
        _validator = validator;
        _telemetry = telemetry;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        if (executionContext.CurrentStage == PipelineStage.Validation)
        {
            if (executionContext.Reasoning == null)
            {
                return Result<ResearchExecutionContext>.Failure(new Error("MissingReasoning", "Reasoning result is missing during validation stage.", ErrorSeverity.Error));
            }

            var startedAt = DateTimeOffset.UtcNow;
            var validation = _validator.ValidateAll(executionContext.Reasoning, executionContext.Context);
            var finishedAt = DateTimeOffset.UtcNow;

            if (!validation.Passed)
            {
                _telemetry.TrackValidationFailure(validation);
            }

            var stageExecution = new PipelineStageExecution(
                Stage: PipelineStage.Validation,
                StartedAt: startedAt,
                FinishedAt: finishedAt,
                Duration: finishedAt - startedAt
            );

            var updatedExecContext = executionContext
                .WithValidation(validation)
                .WithStageExecution(stageExecution)
                .TransitionTo(PipelineStage.Explainability);

            return await next(updatedExecContext);
        }

        return await next(executionContext);
    }
}
