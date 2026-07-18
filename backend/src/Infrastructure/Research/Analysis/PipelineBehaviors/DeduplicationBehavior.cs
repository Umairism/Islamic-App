using System;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class DeduplicationBehavior : IResearchPipelineBehavior
{
    private readonly IEvidenceDeduplicator _deduplicator;

    public DeduplicationBehavior(IEvidenceDeduplicator deduplicator)
    {
        _deduplicator = deduplicator;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        if (executionContext.CurrentStage == PipelineStage.Deduplication)
        {
            var corpus = executionContext.Context.Input.Corpus;
            if (corpus == null)
            {
                return Result<ResearchExecutionContext>.Failure(new Error("MissingCorpus", "Corpus is missing during deduplication stage.", ErrorSeverity.Error));
            }

            var originalCount = corpus.Evidences.Count;
            var dedupedCorpus = _deduplicator.Deduplicate(corpus);
            var updatedContext = executionContext.Context.WithCorpus(dedupedCorpus);

            var dedupeEvent = new EvidenceDeduplicatedEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTimeOffset.UtcNow,
                OriginalCount: originalCount,
                DedupedCount: dedupedCorpus.Evidences.Count
            );

            var updatedExecContext = executionContext
                .WithContext(updatedContext)
                .Raise(dedupeEvent)
                .TransitionTo(PipelineStage.Analysis);

            return await next(updatedExecContext);
        }

        return await next(executionContext);
    }
}
