using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Memory;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Semantic.Query;
using IslamicApp.Infrastructure.Persistence;
using IslamicApp.Infrastructure.Persistence.Entities;
using IslamicApp.Infrastructure.Persistence.EventHandlers;
using IslamicApp.Infrastructure.Research;

namespace IslamicApp.UnitTests;

public class ResearchAsyncAgentTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ResearchQueue_Should_Enqueue_And_Dequeue_Job()
    {
        // Arrange
        var queue = new ResearchQueue();
        var job = new ResearchJob(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        // Action
        await queue.EnqueueAsync(job);
        var dequeued = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.Equal(job.ResearchSessionId, dequeued.ResearchSessionId);
        Assert.Equal(job.WorkspaceId, dequeued.WorkspaceId);
    }

    [Fact]
    public async Task ResearchBackgroundWorker_Should_Recover_Queued_Sessions_At_Startup()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var workspaceId = Guid.NewGuid();
        var session1 = new ResearchSessionEntity
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Title = "Fasting rules",
            Query = "what invalidates fasting?",
            Status = "Queued",
            CreatedAt = DateTimeOffset.UtcNow,
            CurrentStage = "Queueing"
        };
        var session2 = new ResearchSessionEntity
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Title = "Zakat calculations",
            Query = "zakat rules",
            Status = "Completed",
            CreatedAt = DateTimeOffset.UtcNow,
            CurrentStage = "Completed"
        };

        dbContext.ResearchSessions.Add(session1);
        dbContext.ResearchSessions.Add(session2);
        await dbContext.SaveChangesAsync();

        var queue = Substitute.For<IResearchQueue>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(dbContext);
        serviceCollection.AddSingleton(Substitute.For<IMediator>());
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var logger = Substitute.For<ILogger<ResearchBackgroundWorker>>();
        var worker = new ResearchBackgroundWorker(queue, serviceProvider, logger);

        // Action - Run startup recovery via ExecuteAsync (linked token will trigger exit right away)
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Use a private or reflection helper to trigger recovery or invoke ExecuteAsync
        var method = typeof(ResearchBackgroundWorker).GetMethod("RecoverQueuedSessionsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(worker, null);

        // Assert
        await queue.Received(1).EnqueueAsync(Arg.Is<ResearchJob>(j => j.ResearchSessionId == session1.Id));
        await queue.DidNotReceive().EnqueueAsync(Arg.Is<ResearchJob>(j => j.ResearchSessionId == session2.Id));
    }

    [Fact]
    public async Task ResearchStageCompletedHandler_Should_Broadcast_To_SignalR_Group()
    {
        // Arrange
        var hubContext = Substitute.For<IHubContext<ResearchHub>>();
        var clients = Substitute.For<IHubClients>();
        var clientProxy = Substitute.For<IClientProxy>();

        hubContext.Clients.Returns(clients);
        clients.Group(Arg.Any<string>()).Returns(clientProxy);

        var handler = new ResearchStageCompletedHandler(hubContext);
        var sessionId = Guid.NewGuid();
        var stageCompletedEvent = new ResearchStageCompletedEvent(
            sessionId,
            "Retrieval",
            30,
            "Retrieval complete",
            DateTimeOffset.UtcNow
        );

        // Action
        await handler.Handle(stageCompletedEvent, CancellationToken.None);

        // Assert
        clients.Received(1).Group(sessionId.ToString());
        await clientProxy.Received(1).SendCoreAsync(
            "OnStageUpdated",
            Arg.Is<object[]>(args => args.Length == 1 && ((ResearchProgressDto)args[0]).Stage == "Retrieval")
        );
    }

    [Fact]
    public async Task BackgroundWorker_Should_Mark_Session_Failed_On_Exception()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var sessionId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var session = new ResearchSessionEntity
        {
            Id = sessionId,
            WorkspaceId = workspaceId,
            Status = "Queued",
            Query = "Invalid",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.ResearchSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var queue = new ResearchQueue();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(dbContext);
        serviceCollection.AddSingleton(Substitute.For<IMediator>());
        
        // Mock query rewrites / analyzer to throw exception
        var queryAnalyzer = Substitute.For<IQueryAnalyzer>();
        queryAnalyzer.AnalyzeAsync(Arg.Any<SearchRequest>()).Returns(Task.FromException<QueryAnalysis>(new InvalidOperationException("API failure")));
        serviceCollection.AddSingleton(queryAnalyzer);

        var queryRewriter = Substitute.For<IQueryRewriter>();
        serviceCollection.AddSingleton(queryRewriter);

        var pipeline = Substitute.For<IResearchPipeline>();
        serviceCollection.AddSingleton(pipeline);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = Substitute.For<ILogger<ResearchBackgroundWorker>>();
        var worker = new ResearchBackgroundWorker(queue, serviceProvider, logger);

        var job = new ResearchJob(sessionId, workspaceId, DateTimeOffset.UtcNow);

        // Action
        var method = typeof(ResearchBackgroundWorker).GetMethod("ProcessJobAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(worker, new object[] { job, CancellationToken.None });

        // Assert
        var updatedSession = await dbContext.ResearchSessions.FindAsync(sessionId);
        Assert.NotNull(updatedSession);
        Assert.Equal("Failed", updatedSession.Status);
        Assert.Equal("Failed", updatedSession.CurrentStage);
    }

    [Fact]
    public async Task BackgroundWorker_Should_Persist_Iterations_And_Results_On_Success()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var sessionId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var session = new ResearchSessionEntity
        {
            Id = sessionId,
            WorkspaceId = workspaceId,
            Status = "Queued",
            Query = "Preservation of Quran",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.ResearchSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var queue = new ResearchQueue();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(dbContext);
        serviceCollection.AddSingleton(Substitute.For<IMediator>());

        var queryAnalyzer = Substitute.For<IQueryAnalyzer>();
        serviceCollection.AddSingleton(queryAnalyzer);

        var queryRewriter = Substitute.For<IQueryRewriter>();
        queryRewriter.RewriteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new SemanticQuery("rewritten query", new List<string>(), new List<string>(), new List<string>(), 1.0)));
        serviceCollection.AddSingleton(queryRewriter);

        var pipeline = Substitute.For<IResearchPipeline>();
        
        // Stub successful execution result
        var searchReq = new SearchRequest("Preservation of Quran", ResearchLanguage.Auto, new HashSet<EvidenceSource> { EvidenceSource.Quran }, new Pagination(1, 20), true, true, true);
        var queryAnalysis = new QueryAnalysis(
            searchReq,
            new NormalizedQuery("preservation of quran", "preservation of quran", new List<string>(), new List<string>(), new List<string>(), new List<string>()),
            ResearchLanguage.Auto,
            new QueryIntent(SearchMode.KeywordSearch, new HashSet<EvidenceSource>(), new HashSet<RetrievalCapability>(), 1.0),
            null,
            new List<string>()
        );

        queryAnalyzer.AnalyzeAsync(Arg.Any<SearchRequest>()).Returns(Task.FromResult(queryAnalysis));
        
        var execCtx = new ResearchExecutionContext(
            Context: new ResearchContext(new ResearchInput(queryAnalysis)),
            Events: System.Collections.Immutable.ImmutableList<IDomainEvent>.Empty,
            CurrentStage: PipelineStage.Completed,
            StageExecutions: System.Collections.Immutable.ImmutableList<PipelineStageExecution>.Empty,
            Reasoning: new ReasoningResult(
                Summary: "Quran was preserved in written and memorized forms.",
                Claims: new List<ResearchClaim>(),
                Findings: new List<ResearchFinding>(),
                Limitations: new List<ResearchLimitation>(),
                Methodology: ResearchMethodologyType.Thematic,
                PromptVersion: "1.0",
                RawResponse: "response",
                Metadata: new GenerationMetadata("provider", "model", 100, 100, TimeSpan.FromSeconds(1), false, FinishReason.Stop)
            ),
            Iteration: new IterationContext(
                CurrentIteration: 1,
                State: PipelineState.Completed,
                Confidence: new CompositeConfidence(0.95, 0.90, 0.95, 0.90, 1.0),
                History: System.Collections.Immutable.ImmutableList.Create(new IterationRecord(
                    Iteration: 1,
                    RetrievedNodes: new List<string> { "node-1" },
                    NewEvidence: new List<string> { "ref-1" },
                    ConfidenceResult: new ConfidenceResult(0.92, new Dictionary<string, double>(), "explanation"),
                    KnowledgeGaps: new List<EvidenceGap>(),
                    Duration: TimeSpan.FromSeconds(2)
                )),
                PendingGaps: new List<EvidenceGap>(),
                RetrievedEvidence: new List<string> { "ref-1" }
            )
        );

        pipeline.ExecuteAsync(Arg.Any<QueryAnalysis>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ResearchExecutionContext>.Success(execCtx));

        serviceCollection.AddSingleton(pipeline);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = Substitute.For<ILogger<ResearchBackgroundWorker>>();
        var worker = new ResearchBackgroundWorker(queue, serviceProvider, logger);

        var job = new ResearchJob(sessionId, workspaceId, DateTimeOffset.UtcNow);

        // Action
        var method = typeof(ResearchBackgroundWorker).GetMethod("ProcessJobAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(worker, new object[] { job, CancellationToken.None });

        // Assert
        var updatedSession = await dbContext.ResearchSessions
            .Include(s => s.Iterations)
            .Include(s => s.Results)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        Assert.NotNull(updatedSession);
        Assert.Equal("Completed", updatedSession.Status);
        Assert.Equal(1, updatedSession.Iterations.Count);
        Assert.Equal("node-1", JsonSerializerDeserializerHelper(updatedSession.Iterations.First().RetrievedNodesJson).First());
        Assert.Equal(1, updatedSession.Results.Count);
        Assert.Equal("Quran was preserved in written and memorized forms.", updatedSession.Results.First().AnswerText);
        Assert.True(updatedSession.Results.First().IsFinal);
    }

    [Fact]
    public async Task ExecuteQuery_CircumcisionRuling_ReturnsDossierAndSummary()
    {
        // Arrange
        var dbContext = CreateInMemoryDbContext();
        var sessionId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var session = new ResearchSessionEntity
        {
            Id = sessionId,
            WorkspaceId = workspaceId,
            Title = "Circumcision Ruling in Islam",
            Query = "What are the ruling about circumcision in Islam?",
            Status = "Queued",
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.ResearchSessions.Add(session);
        await dbContext.SaveChangesAsync();

        var queue = new ResearchQueue();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(dbContext);
        serviceCollection.AddSingleton(Substitute.For<IMediator>());

        var queryAnalyzer = Substitute.For<IQueryAnalyzer>();
        var queryRewriter = Substitute.For<IQueryRewriter>();
        queryRewriter.RewriteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new SemanticQuery("circumcision ruling in islam khitan fitrah wajib sunnah", new List<string>(), new List<string>(), new List<string>(), 1.0)));
        
        serviceCollection.AddSingleton(queryAnalyzer);
        serviceCollection.AddSingleton(queryRewriter);

        var pipeline = Substitute.For<IResearchPipeline>();
        
        var searchReq = new SearchRequest("What are the ruling about circumcision in Islam?", ResearchLanguage.Auto, new HashSet<EvidenceSource> { EvidenceSource.Quran, EvidenceSource.Hadith }, new Pagination(1, 20), true, true, true);
        var queryAnalysis = new QueryAnalysis(
            searchReq,
            new NormalizedQuery("circumcision ruling in islam", "circumcision ruling in islam", new List<string>(), new List<string>(), new List<string>(), new List<string>()),
            ResearchLanguage.Auto,
            new QueryIntent(SearchMode.KeywordSearch, new HashSet<EvidenceSource>(), new HashSet<RetrievalCapability>(), 1.0),
            null,
            new List<string>()
        );
        queryAnalyzer.AnalyzeAsync(Arg.Any<SearchRequest>()).Returns(Task.FromResult(queryAnalysis));

        var answerSummary = "Circumcision (Khitan) is a fundamental practice in Islam originating from the Fitrah (natural human disposition) and the tradition (Millah) of Prophet Ibrahim (peace be upon him).\n\n" +
            "### Primary Sources & Evidence\n" +
            "1. **Sunnah / Hadith**: Narrated by Abu Hurairah (RA), the Messenger of Allah (ﷺ) said: 'Five acts are of the Fitrah: circumcision, shaving pubic hair, trimming the mustache, clipping nails, and plucking armpit hair.' (*Sahih al-Bukhari 5889, Sahih Muslim 257*).\n" +
            "2. **Quranic Tradition**: Allah Almighty commands in Surah An-Nahl (16:123): 'Then We revealed to you [O Muhammad] to follow the religion of Abraham, inclination toward truth...'\n\n" +
            "### Scholarly Consensus & Juristic Rulings (Fiqh)\n" +
            "- **Shafi'i & Hanbali Schools**: Obligatory (*Wajib*) for males as a fundamental emblem of purification and prayer validity.\n" +
            "- **Hanafi & Maliki Schools**: Highly Emphasized Sunnah (*Sunnah Mu'akkadah*) and an essential Islamic symbol (*Sha'irah*).";

        var execCtx = new ResearchExecutionContext(
            Context: new ResearchContext(new ResearchInput(queryAnalysis)),
            Events: System.Collections.Immutable.ImmutableList<IDomainEvent>.Empty,
            CurrentStage: PipelineStage.Completed,
            StageExecutions: System.Collections.Immutable.ImmutableList<PipelineStageExecution>.Empty,
            Reasoning: new ReasoningResult(
                Summary: answerSummary,
                Claims: new List<ResearchClaim>
                {
                    new ResearchClaim("Circumcision is among the five acts of Fitrah", new List<ReferenceId> { new ReferenceId("Bukhari-5889"), new ReferenceId("Muslim-257") }, new ConfidenceScore(0.98), ClaimType.LegalRuling, ClaimOrigin.DirectEvidence),
                    new ResearchClaim("Shafi'i and Hanbali jurists consider Khitan Wajib for men", new List<ReferenceId> { new ReferenceId("Fiqh-Al-Islami-1/450") }, new ConfidenceScore(0.95), ClaimType.LegalRuling, ClaimOrigin.MultiEvidenceInference)
                },
                Findings: new List<ResearchFinding>(),
                Limitations: new List<ResearchLimitation>(),
                Methodology: ResearchMethodologyType.Fiqh,
                PromptVersion: "1.0",
                RawResponse: answerSummary,
                Metadata: new GenerationMetadata("provider", "model", 150, 250, TimeSpan.FromSeconds(1.2), false, FinishReason.Stop)
            ),
            Iteration: new IterationContext(
                CurrentIteration: 1,
                State: PipelineState.Completed,
                Confidence: new CompositeConfidence(0.98, 0.95, 0.96, 0.94, 1.0),
                History: System.Collections.Immutable.ImmutableList.Create(new IterationRecord(
                    Iteration: 1,
                    RetrievedNodes: new List<string> { "Hadith-Bukhari-5889", "Hadith-Muslim-257", "Quran-16-123" },
                    NewEvidence: new List<string> { "Ref-Fiqh-1" },
                    ConfidenceResult: new ConfidenceResult(0.96, new Dictionary<string, double> { { "Evidence", 0.98 }, { "Citation", 0.95 } }, "High scholarly consensus and direct Fitrah Hadith evidence."),
                    KnowledgeGaps: new List<EvidenceGap>(),
                    Duration: TimeSpan.FromSeconds(1.2)
                )),
                PendingGaps: new List<EvidenceGap>(),
                RetrievedEvidence: new List<string> { "Bukhari-5889", "Muslim-257", "Quran-16-123" }
            )
        );

        pipeline.ExecuteAsync(Arg.Any<QueryAnalysis>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result<ResearchExecutionContext>.Success(execCtx));

        serviceCollection.AddSingleton(pipeline);

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = Substitute.For<ILogger<ResearchBackgroundWorker>>();
        var worker = new ResearchBackgroundWorker(queue, serviceProvider, logger);

        var job = new ResearchJob(sessionId, workspaceId, DateTimeOffset.UtcNow);

        // Action
        var method = typeof(ResearchBackgroundWorker).GetMethod("ProcessJobAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(worker, new object[] { job, CancellationToken.None });

        // Assert
        var result = await dbContext.ResearchResults.FirstOrDefaultAsync(r => r.ResearchSessionId == sessionId);
        Assert.NotNull(result);
        Assert.Contains("Fitrah", result.AnswerText);
        Assert.Contains("Shafi'i", result.AnswerText);
        Assert.Equal(0.98, result.ConfidenceScore);
    }

    private List<string> JsonSerializerDeserializerHelper(string json)
    {
        return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
    }
}
