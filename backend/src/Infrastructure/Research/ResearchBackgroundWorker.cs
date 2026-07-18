using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Memory;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Semantic.Query;
using IslamicApp.Infrastructure.Persistence;
using IslamicApp.Infrastructure.Persistence.Entities;

namespace IslamicApp.Infrastructure.Research;

public class ResearchBackgroundWorker : BackgroundService
{
    private readonly IResearchQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ResearchBackgroundWorker> _logger;

    public ResearchBackgroundWorker(
        IResearchQueue queue,
        IServiceProvider serviceProvider,
        ILogger<ResearchBackgroundWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Research background worker starting...");

        // 1. Recover queued sessions from database
        try
        {
            await RecoverQueuedSessionsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover queued sessions during background worker startup.");
        }

        // 2. Consume execution channel
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                _logger.LogInformation("Processing research session {SessionId} from queue.", job.ResearchSessionId);

                await ProcessJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued research job.");
            }
        }

        _logger.LogInformation("Research background worker stopped.");
    }

    private async Task RecoverQueuedSessionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var queuedSessions = await dbContext.ResearchSessions
            .Where(s => s.Status == "Queued")
            .ToListAsync();

        if (queuedSessions.Count > 0)
        {
            _logger.LogInformation("Found {Count} queued sessions to recover.", queuedSessions.Count);
            foreach (var session in queuedSessions)
            {
                await _queue.EnqueueAsync(new ResearchJob(session.Id, session.WorkspaceId, DateTimeOffset.UtcNow));
            }
        }
    }

    private async Task ProcessJobAsync(ResearchJob job, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Load active session with concurrency check
        var session = await dbContext.ResearchSessions
            .FirstOrDefaultAsync(s => s.Id == job.ResearchSessionId);

        if (session == null)
        {
            _logger.LogWarning("Research session {SessionId} not found in database. Skipping job.", job.ResearchSessionId);
            return;
        }

        if (session.Status == "Cancelled")
        {
            _logger.LogInformation("Research session {SessionId} was already cancelled. Skipping job.", job.ResearchSessionId);
            return;
        }

        // Transition to Running
        session.Status = "Running";
        session.CurrentStage = "Retrieval";
        await dbContext.SaveChangesAsync();

        // Publish session started event
        await mediator.Publish(new ResearchSessionStartedEvent(session.Id, session.WorkspaceId, DateTimeOffset.UtcNow));

        // Create linked cancellation token source
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        try
        {
            // Set up Pipeline stage execution
            var queryAnalyzer = scope.ServiceProvider.GetRequiredService<IQueryAnalyzer>();
            var queryRewriter = scope.ServiceProvider.GetRequiredService<IQueryRewriter>();
            var pipeline = scope.ServiceProvider.GetRequiredService<IResearchPipeline>();

            var searchReq = new SearchRequest(
                Query: session.Query,
                Language: Application.Research.Enums.ResearchLanguage.Auto,
                Sources: new HashSet<Application.Research.Enums.EvidenceSource> { Application.Research.Enums.EvidenceSource.Quran, Application.Research.Enums.EvidenceSource.Hadith },
                Pagination: new Pagination(1, 20),
                IncludeCrossReferences: true,
                IncludeExplanations: true,
                SemanticSearchEnabled: true
            );

            var queryAnalysis = await queryAnalyzer.AnalyzeAsync(searchReq);
            var rewritten = await queryRewriter.RewriteAsync(session.Query, cts.Token);
            queryAnalysis = queryAnalysis with { SemanticQuery = rewritten };

            // Execute actual pipeline
            var pipeResult = await pipeline.ExecuteAsync(queryAnalysis, session.Id, session.WorkspaceId, cts.Token);

            if (!pipeResult.IsSuccess)
            {
                throw new InvalidOperationException(pipeResult.Error!.Message);
            }

            var execCtx = pipeResult.Value!;

            // Write iterations history
            if (execCtx.Iteration != null)
            {
                foreach (var record in execCtx.Iteration.History)
                {
                    var iterationEntity = new ResearchIterationEntity
                    {
                        Id = Guid.NewGuid(),
                        ResearchSessionId = session.Id,
                        IterationNumber = record.Iteration,
                        PipelineStage = "Reasoning",
                        ConfidenceScore = record.ConfidenceResult.Score,
                        GapsJson = JsonSerializer.Serialize(record.KnowledgeGaps),
                        RetrievedNodesJson = JsonSerializer.Serialize(record.RetrievedNodes),
                        NewEvidenceJson = JsonSerializer.Serialize(record.NewEvidence),
                        DurationMs = record.Duration.TotalMilliseconds,
                        CreatedAt = DateTimeOffset.UtcNow
                    };
                    dbContext.ResearchIterations.Add(iterationEntity);
                }
            }

            // Save versioned result
            var resultEntity = new ResearchResultEntity
            {
                Id = Guid.NewGuid(),
                ResearchSessionId = session.Id,
                AnswerText = execCtx.Reasoning?.Summary ?? "No summary generated.",
                ConfidenceScore = execCtx.Iteration?.Confidence.Evidence ?? 1.0,
                CitationsJson = JsonSerializer.Serialize(execCtx.Reasoning?.Claims.SelectMany(c => c.SupportingEvidence).Select(e => e.Value).ToList() ?? new List<string>()),
                Version = (execCtx.Iteration?.CurrentIteration ?? 0) + 1,
                IsFinal = true,
                GeneratedAt = DateTimeOffset.UtcNow
            };
            dbContext.ResearchResults.Add(resultEntity);

            // Complete session
            session.Status = "Completed";
            session.CurrentStage = "Completed";
            session.ConfidenceValue = resultEntity.ConfidenceScore;
            session.ConfidenceLevel = resultEntity.ConfidenceScore >= 0.85 ? "High" : "Normal";

            await dbContext.SaveChangesAsync();

            // Publish completed event
            await mediator.Publish(new ResearchSessionCompletedEvent(session.Id, session.WorkspaceId, DateTimeOffset.UtcNow));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Research session {SessionId} cancellation requested.", session.Id);
            session.Status = "Cancelled";
            session.CurrentStage = "Cancelled";
            await dbContext.SaveChangesAsync();

            await mediator.Publish(new ResearchSessionCancelledEvent(session.Id, session.WorkspaceId, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pipeline execution failed for session {SessionId}.", session.Id);
            session.Status = "Failed";
            session.CurrentStage = "Failed";
            await dbContext.SaveChangesAsync();

            await mediator.Publish(new ResearchSessionFailedEvent(session.Id, session.WorkspaceId, ex.Message, DateTimeOffset.UtcNow));
        }
    }
}
