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

public class DossierGenerationBehavior : IResearchPipelineBehavior
{
    private readonly IDossierGenerator _dossierGenerator;
    private readonly IResearchEvaluator _evaluator;
    private readonly ApplicationDbContext _dbContext;

    public DossierGenerationBehavior(
        IDossierGenerator dossierGenerator,
        IResearchEvaluator evaluator,
        ApplicationDbContext dbContext)
    {
        _dossierGenerator = dossierGenerator;
        _evaluator = evaluator;
        _dbContext = dbContext;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        if (executionContext.CurrentStage == PipelineStage.DossierGeneration)
        {
            var startedAt = DateTimeOffset.UtcNow;
            var sessionId = executionContext.Session?.SessionId ?? Guid.NewGuid();

            var evaluation = await _evaluator.EvaluateAsync(executionContext, cancellationToken);
            var dossierResult = await _dossierGenerator.GenerateAsync(executionContext, evaluation, cancellationToken);

            // Persist ResearchDossierEntity
            var dossierEntity = new ResearchDossierEntity
            {
                Id = Guid.NewGuid(),
                ResearchSessionId = sessionId,
                Format = "Markdown",
                ContentHash = dossierResult.ContentHash,
                StoragePath = dossierResult.StoragePath,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.ResearchDossiers.Add(dossierEntity);

            // Audit Event: DossierGenerated
            var eventEntity = new ResearchEventEntity
            {
                Id = Guid.NewGuid(),
                ResearchSessionId = sessionId,
                EventType = "DossierGenerated",
                PayloadJson = JsonSerializer.Serialize(new
                {
                    Format = "Markdown",
                    ContentHash = dossierResult.ContentHash
                }),
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.ResearchEvents.Add(eventEntity);

            await _dbContext.SaveChangesAsync(cancellationToken);

            var finishedAt = DateTimeOffset.UtcNow;
            var stageExecution = new PipelineStageExecution(
                Stage: PipelineStage.DossierGeneration,
                StartedAt: startedAt,
                FinishedAt: finishedAt,
                Duration: finishedAt - startedAt
            );

            var updatedExecContext = executionContext
                .WithStageExecution(stageExecution)
                .TransitionTo(PipelineStage.Completed);

            return await next(updatedExecContext);
        }

        return await next(executionContext);
    }
}
