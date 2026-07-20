using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Evaluation;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Persistence;
using IslamicApp.Infrastructure.Persistence.Entities;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class EvaluationBehavior : IResearchPipelineBehavior
{
    private readonly IResearchEvaluator _evaluator;
    private readonly ApplicationDbContext _dbContext;

    public EvaluationBehavior(
        IResearchEvaluator evaluator,
        ApplicationDbContext dbContext)
    {
        _evaluator = evaluator;
        _dbContext = dbContext;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        if (executionContext.CurrentStage == PipelineStage.Evaluation)
        {
            var startedAt = DateTimeOffset.UtcNow;

            var evalResult = await _evaluator.EvaluateAsync(executionContext, cancellationToken);

            var sessionId = executionContext.Session?.SessionId ?? Guid.NewGuid();

            // Persist ResearchEvaluationEntity
            var evalEntity = new ResearchEvaluationEntity
            {
                Id = Guid.NewGuid(),
                ResearchSessionId = sessionId,
                OverallScore = evalResult.Score.OverallScore,
                EvidenceCoverage = evalResult.Score.EvidenceCoverage,
                CitationAccuracy = evalResult.Score.CitationAccuracy,
                ReasoningConsistency = evalResult.Score.ReasoningConsistency,
                SourceDiversity = evalResult.Score.SourceDiversity,
                MetricsJson = JsonSerializer.Serialize(evalResult.Score),
                FindingsJson = JsonSerializer.Serialize(evalResult.Findings),
                EvaluationVersion = evalResult.EvaluationVersion,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.ResearchEvaluations.Add(evalEntity);

            // Audit Event: EvaluationCompleted
            var eventEntity = new ResearchEventEntity
            {
                Id = Guid.NewGuid(),
                ResearchSessionId = sessionId,
                EventType = "EvaluationCompleted",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    Score = evalResult.Score.OverallScore,
                    Version = evalResult.EvaluationVersion
                }),
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.ResearchEvents.Add(eventEntity);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var finishedAt = DateTimeOffset.UtcNow;
            var stageExecution = new PipelineStageExecution(
                Stage: PipelineStage.Evaluation,
                StartedAt: startedAt,
                FinishedAt: finishedAt,
                Duration: finishedAt - startedAt
            );

            var updatedExecContext = executionContext
                .WithStageExecution(stageExecution)
                .TransitionTo(PipelineStage.Rendering);

            return await next(updatedExecContext);
        }

        return await next(executionContext);
    }
}
