using System;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class PersistenceBehavior : IResearchPipelineBehavior
{
    private readonly IOutboxService _outboxService;
    private readonly IResearchFeatureFlags _featureFlags;

    public PersistenceBehavior(IOutboxService outboxService, IResearchFeatureFlags featureFlags)
    {
        _outboxService = outboxService;
        _featureFlags = featureFlags;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        System.Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        var result = await next(executionContext);
        if (!result.IsSuccess) return result;

        if (!_featureFlags.EnableWorkspacePersistence)
        {
            return result;
        }

        var execContext = result.Value!;

        // Resolve workspace (default workspace Guid if not specified)
        var workspaceId = Guid.Empty; // Using default workspace
        var sessionId = Guid.NewGuid(); // Unique research session Guid

        // 1. Write ResearchStartedEvent
        var startedEvent = new ResearchStartedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            SessionId: sessionId,
            Query: execContext.Context.Input.Query.OriginalRequest.Query,
            WorkspaceId: workspaceId
        );
        await _outboxService.WriteEventAsync(startedEvent, cancellationToken);

        // 2. Write ResearchExecutedEvent (if execution happened)
        if (execContext.Session != null)
        {
            var executedEvent = new ResearchExecutedEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTimeOffset.UtcNow,
                SessionId: sessionId,
                WorkspaceId: workspaceId,
                Prompt: execContext.Session.Prompt,
                Response: execContext.Session.Response,
                Metadata: execContext.Session.Metadata
            );
            await _outboxService.WriteEventAsync(executedEvent, cancellationToken);
        }

        // 3. Write ResearchValidatedEvent (if validation happened)
        if (execContext.Validation != null)
        {
            var validatedEvent = new ResearchValidatedEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTimeOffset.UtcNow,
                SessionId: sessionId,
                WorkspaceId: workspaceId,
                Reasoning: execContext.Reasoning,
                Report: execContext.Validation,
                Explainability: execContext.Explainability
            );
            await _outboxService.WriteEventAsync(validatedEvent, cancellationToken);

            if (!execContext.Validation.Passed)
            {
                var failedEvent = new ValidationFailedEvent(
                    EventId: Guid.NewGuid(),
                    OccurredAt: DateTimeOffset.UtcNow,
                    SessionId: sessionId,
                    WorkspaceId: workspaceId,
                    Report: execContext.Validation
                );
                await _outboxService.WriteEventAsync(failedEvent, cancellationToken);
            }
        }

        // 4. Write ResearchPublishedEvent (if publishable outputs were rendered)
        if (execContext.Validation != null && execContext.Validation.Passed && execContext.RenderedOutputs != null && execContext.RenderedOutputs.Count > 0)
        {
            var documentId = Guid.NewGuid();
            var revisionId = Guid.NewGuid();
            var publishedEvent = new ResearchPublishedEvent(
                EventId: Guid.NewGuid(),
                OccurredAt: DateTimeOffset.UtcNow,
                SessionId: sessionId,
                WorkspaceId: workspaceId,
                DocumentId: documentId,
                RevisionId: revisionId,
                Summary: execContext.Reasoning?.Summary ?? string.Empty
            );
            await _outboxService.WriteEventAsync(publishedEvent, cancellationToken);
        }

        return result;
    }
}
