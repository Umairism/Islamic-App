using System;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class AnalysisBehavior : IResearchPipelineBehavior
{
    private readonly IResearchAnalysisBuilder _analysisBuilder;

    public AnalysisBehavior(IResearchAnalysisBuilder analysisBuilder)
    {
        _analysisBuilder = analysisBuilder;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        if (executionContext.CurrentStage == PipelineStage.Analysis)
        {
            var corpus = executionContext.Context.Input.Corpus;
            if (corpus == null)
            {
                return Result<ResearchExecutionContext>.Failure(new Error("MissingCorpus", "Corpus is missing during analysis stage.", ErrorSeverity.Error));
            }

            var startedAt = DateTimeOffset.UtcNow;
            var analysis = _analysisBuilder.Build(executionContext.Context.Input.Query, corpus);
            var finishedAt = DateTimeOffset.UtcNow;

            var updatedContext = executionContext.Context.WithAnalysis(analysis);

            var graphEvent = new GraphBuiltEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTimeOffset.UtcNow,
                NodeCount: analysis.Graph.Nodes.Count,
                EdgeCount: analysis.Graph.Relationships.Count
            );

            var conflictEvent = new ConflictDetectedEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTimeOffset.UtcNow,
                ConflictCount: analysis.Conflicts.Conflicts.Count
            );

            var methodologyEvent = new MethodologySelectedEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTimeOffset.UtcNow,
                MethodologyType: analysis.Methodology.Type
            );

            var stageExecution = new PipelineStageExecution(
                Stage: PipelineStage.Analysis,
                StartedAt: startedAt,
                FinishedAt: finishedAt,
                Duration: finishedAt - startedAt
            );

            var updatedExecContext = executionContext
                .WithContext(updatedContext)
                .Raise(graphEvent)
                .Raise(conflictEvent)
                .Raise(methodologyEvent)
                .WithStageExecution(stageExecution)
                .TransitionTo(PipelineStage.Reasoning);

            return await next(updatedExecContext);
        }

        return await next(executionContext);
    }
}
