using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Memory;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Persistence.EventHandlers;

public class MemoryEventHandlers : INotificationHandler<ResearchValidatedEvent>
{
    private readonly IMemoryCompressor _compressor;
    private readonly IKnowledgeMemoryStore _memoryStore;
    private readonly ILogger<MemoryEventHandlers> _logger;

    public MemoryEventHandlers(
        IMemoryCompressor compressor,
        IKnowledgeMemoryStore memoryStore,
        ILogger<MemoryEventHandlers> logger)
    {
        _compressor = compressor;
        _memoryStore = memoryStore;
        _logger = logger;
    }

    public async Task Handle(ResearchValidatedEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.Report.Passed)
        {
            _logger.LogWarning("Research session {SessionId} validation failed. Skipping knowledge memory capture.", notification.SessionId);
            return;
        }

        _logger.LogInformation("Compressing and storing knowledge memory for session {SessionId}", notification.SessionId);

        // Build a mock/minimal ResearchResult to pass to the compressor
        var input = new ResearchInput(
            new QueryAnalysis(
                new SearchRequest(notification.Reasoning.Summary, ResearchLanguage.English, null, null, false, false, false),
                null!,
                ResearchLanguage.English,
                null!,
                null!,
                null!
            ),
            null
        );

        var context = new ResearchContext(input);
        var execContext = new ResearchExecutionContext(context, System.Collections.Immutable.ImmutableList<IDomainEvent>.Empty, PipelineStage.Completed, System.Collections.Immutable.ImmutableList<PipelineStageExecution>.Empty)
            .WithReasoning(
                new ReasoningSession(notification.SessionId, null!, null!, null!, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow),
                notification.Reasoning
            );

        var result = new ResearchResult(
            ExecutionContext: execContext,
            Session: execContext.Session,
            Reasoning: notification.Reasoning,
            Validation: notification.Report,
            Explainability: notification.Explainability,
            Outputs: new[] { new RenderResult("Mock content", "text/plain", ".txt", "render.txt") }
        );

        var memoryEntry = await _compressor.CompressAsync(result, cancellationToken);

        // Map correct WorkspaceId and Session details from the validated event
        var finalEntry = memoryEntry with
        {
            WorkspaceId = notification.WorkspaceId,
            OriginSessionId = notification.SessionId,
            Confidence = new CompositeConfidence(
                Evidence: notification.Report.ClaimValidation.Passed ? 1.0 : 0.8,
                Citation: notification.Report.CitationValidation.Passed ? 1.0 : 0.8,
                Validation: notification.Report.Passed ? 1.0 : 0.5,
                Reasoning: 0.95,
                Methodology: 1.0
            )
        };

        await _memoryStore.StoreAsync(finalEntry, cancellationToken);
        _logger.LogInformation("Knowledge memory stored successfully for session {SessionId}.", notification.SessionId);
    }
}
