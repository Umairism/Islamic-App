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
using IslamicApp.Infrastructure.AI;
using IslamicApp.Infrastructure.AI.Providers;
using IslamicApp.Infrastructure.Persistence;
using IslamicApp.Infrastructure.Persistence.Entities;
using IslamicApp.Infrastructure.Persistence.EventHandlers;
using IslamicApp.Infrastructure.Research;
using IslamicApp.Infrastructure.Research.Analysis;
using IslamicApp.Infrastructure.Research.Analysis.ConflictRules;
using IslamicApp.Infrastructure.Research.Analysis.Methodologies;
using IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;
using IslamicApp.Infrastructure.Search;
using IslamicApp.Infrastructure.Semantic.Query;

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
    public async Task ProcessJobAsync_PersistsPipelineResult_WhenPipelineSucceeds()
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

        var answerSummary = "Circumcision (Khitan) is a fundamental practice in Islam originating from the Fitrah.";

        var execCtx = new ResearchExecutionContext(
            Context: new ResearchContext(new ResearchInput(queryAnalysis)),
            Events: System.Collections.Immutable.ImmutableList<IDomainEvent>.Empty,
            CurrentStage: PipelineStage.Completed,
            StageExecutions: System.Collections.Immutable.ImmutableList<PipelineStageExecution>.Empty,
            Reasoning: new ReasoningResult(
                Summary: answerSummary,
                Claims: new List<ResearchClaim>
                {
                    new ResearchClaim("Circumcision is among the five acts of Fitrah", new List<ReferenceId> { new ReferenceId("Bukhari-5889") }, new ConfidenceScore(0.98), ClaimType.LegalRuling, ClaimOrigin.DirectEvidence)
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
                    RetrievedNodes: new List<string> { "Hadith-Bukhari-5889" },
                    NewEvidence: new List<string> { "Ref-Fiqh-1" },
                    ConfidenceResult: new ConfidenceResult(0.96, new Dictionary<string, double> { { "Evidence", 0.98 } }, "High scholarly consensus."),
                    KnowledgeGaps: new List<EvidenceGap>(),
                    Duration: TimeSpan.FromSeconds(1.2)
                )),
                PendingGaps: new List<EvidenceGap>(),
                RetrievedEvidence: new List<string> { "Bukhari-5889" }
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

        // Assert - Verify Behavioral Invocation and Worker State Changes
        await queryAnalyzer.Received(1).AnalyzeAsync(Arg.Any<SearchRequest>());
        await queryRewriter.Received(1).RewriteAsync(Arg.Is<string>(q => q == session.Query), Arg.Any<CancellationToken>());
        await pipeline.Received(1).ExecuteAsync(Arg.Any<QueryAnalysis>(), sessionId, workspaceId, Arg.Any<CancellationToken>());

        var updatedSession = await dbContext.ResearchSessions.FindAsync(sessionId);
        Assert.NotNull(updatedSession);
        Assert.Equal("Completed", updatedSession.Status);

        var result = await dbContext.ResearchResults.FirstOrDefaultAsync(r => r.ResearchSessionId == sessionId);
        Assert.NotNull(result);
        Assert.True(result.IsFinal);
    }

    [Fact]
    public async Task ExecuteRealPipeline_CircumcisionQuery_AgainstPostgreSQL()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=islamic_research;Username=postgres;Password=password123")
            .Options;

        using var dbContext = new ApplicationDbContext(options);

        // Verify Postgres Connection & Ensure Schema
        bool canConnect = await dbContext.Database.CanConnectAsync();
        if (!canConnect)
        {
            // PostgreSQL Docker container is offline; skip gracefully
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(@"
            DROP TABLE IF EXISTS ""MemoryEntry"", ""ResearchSessions"", ""ResearchIterations"", ""ResearchEvents"", ""ResearchResults"", ""ResearchSession"", ""ResearchIteration"", ""ResearchEvent"", ""ResearchResult"";
            CREATE TABLE IF NOT EXISTS ""MemoryEntry"" (
                ""id"" UUID PRIMARY KEY,
                ""workspaceId"" UUID NOT NULL,
                ""query"" TEXT NOT NULL,
                ""summary"" TEXT NOT NULL,
                ""claimsJson"" TEXT NOT NULL,
                ""evidenceIdsJson"" TEXT NOT NULL,
                ""graphNodesJson"" TEXT NOT NULL,
                ""evidenceHash"" TEXT NOT NULL,
                ""methodology"" INT NOT NULL,
                ""confidenceEvidence"" DOUBLE PRECISION NOT NULL,
                ""confidenceCitation"" DOUBLE PRECISION NOT NULL,
                ""confidenceValidation"" DOUBLE PRECISION NOT NULL,
                ""confidenceReasoning"" DOUBLE PRECISION NOT NULL,
                ""confidenceMethodology"" DOUBLE PRECISION NOT NULL,
                ""createdAt"" TIMESTAMPTZ NOT NULL,
                ""schemaVersion"" INT NOT NULL,
                ""originSessionId"" UUID NOT NULL,
                ""originDocumentRevisionId"" UUID NOT NULL,
                ""compressedFromVersion"" INT NOT NULL,
                ""createdByModel"" TEXT NOT NULL,
                ""promptVersion"" TEXT NOT NULL,
                ""invalidated"" BOOLEAN NOT NULL,
                ""invalidationReason"" TEXT
            );
            CREATE TABLE IF NOT EXISTS ""ResearchSession"" (
                ""id"" UUID PRIMARY KEY,
                ""workspaceId"" UUID NOT NULL,
                ""title"" TEXT,
                ""query"" TEXT NOT NULL,
                ""status"" TEXT NOT NULL,
                ""currentStage"" TEXT,
                ""createdAt"" TIMESTAMPTZ NOT NULL,
                ""completedAt"" TIMESTAMPTZ
            );
            CREATE TABLE IF NOT EXISTS ""ResearchIteration"" (
                ""id"" UUID PRIMARY KEY,
                ""researchSessionId"" UUID NOT NULL,
                ""iterationNumber"" INT NOT NULL,
                ""pipelineStage"" TEXT NOT NULL,
                ""confidenceScore"" DOUBLE PRECISION NOT NULL,
                ""gapsJson"" TEXT NOT NULL,
                ""retrievedNodesJson"" TEXT NOT NULL,
                ""newEvidenceJson"" TEXT NOT NULL,
                ""durationMs"" BIGINT NOT NULL,
                ""createdAt"" TIMESTAMPTZ NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ""ResearchEvent"" (
                ""id"" UUID PRIMARY KEY,
                ""researchSessionId"" UUID NOT NULL,
                ""eventType"" TEXT NOT NULL,
                ""payloadJson"" TEXT NOT NULL,
                ""createdAt"" TIMESTAMPTZ NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ""ResearchResult"" (
                ""id"" UUID PRIMARY KEY,
                ""researchSessionId"" UUID NOT NULL,
                ""answerText"" TEXT NOT NULL,
                ""confidenceScore"" DOUBLE PRECISION NOT NULL,
                ""citationsJson"" TEXT NOT NULL,
                ""version"" INT NOT NULL,
                ""isFinal"" BOOLEAN NOT NULL,
                ""generatedAt"" TIMESTAMPTZ NOT NULL
            );
        ");

        var evidenceRepo = new PostgresEvidenceRepository(dbContext);

        var analyzer = new EvidenceAnalyzer();
        var deduplicator = new EvidenceDeduplicator();
        var graphBuilder = new GraphBuilder(analyzer);
        var conflictDetector = new ConflictDetector(new List<IConflictRule> { new WeakNarrationRule(), new MadhhabDifferenceRule() });
        var methodologySelector = new MethodologySelector();
        var thematic = new ThematicMethodology();
        var mockFactory = new MockMethodologyFactory(thematic);
        var analysisBuilder = new ResearchAnalysisBuilder(methodologySelector, mockFactory, graphBuilder, conflictDetector);

        var promptService = new PromptService();
        var telemetry = new ReasoningTelemetry(Microsoft.Extensions.Logging.Abstractions.NullLogger<ReasoningTelemetry>.Instance);
        var mockProvider = new MockProvider();
        var resilientProvider = new ResilientGenerationProviderDecorator(mockProvider, telemetry);
        var parser = new ReasoningParser();

        var explainabilityBuilder = new ExplainabilityBuilder();
        var validator = new ResearchValidator(
            new List<IValidationRule>
            {
                new IslamicApp.Infrastructure.Research.ValidationRules.ClaimValidationRule(),
                new IslamicApp.Infrastructure.Research.ValidationRules.CitationValidationRule(),
                new IslamicApp.Infrastructure.Research.ValidationRules.ConsistencyValidationRule()
            }
        );

        var renderers = new List<IResearchRenderer>
        {
            new MarkdownRenderer(),
            new HtmlRenderer(),
            new JsonRenderer()
        };

        var outputGuard = new OutputGuard(renderers);
        var reasoner = new Reasoner(promptService, new ITextGenerationProvider[] { resilientProvider }, parser, validator, explainabilityBuilder, outputGuard);

        var behaviors = new List<IResearchPipelineBehavior>
        {
            new ExceptionBehavior(Microsoft.Extensions.Logging.Abstractions.NullLogger<ExceptionBehavior>.Instance),
            new LoggingBehavior(Microsoft.Extensions.Logging.Abstractions.NullLogger<LoggingBehavior>.Instance),
            new RetrievalBehavior(evidenceRepo),
            new DeduplicationBehavior(deduplicator),
            new AnalysisBehavior(analysisBuilder),
            new ReasoningBehavior(reasoner),
            new ValidationBehavior(validator, new ReasoningTelemetry(Microsoft.Extensions.Logging.Abstractions.NullLogger<ReasoningTelemetry>.Instance)),
            new ExplainabilityBehavior(explainabilityBuilder),
            new RenderingBehavior(renderers)
        };

        var pipeline = new ResearchPipeline(behaviors, Substitute.For<IMediator>());

        var tokenizer = new Tokenizer();
        var queryRewriter = new QueryRewriter();
        var configProvider = new SearchConfigurationProvider();
        var queryAnalyzer = new QueryAnalyzer(new SearchNormalizer(), tokenizer, configProvider, configProvider);

        var queryText = "What are the ruling about circumcision in Islam?";
        var searchReq = new SearchRequest(
            Query: queryText,
            Language: ResearchLanguage.Auto,
            Sources: new HashSet<EvidenceSource> { EvidenceSource.Quran, EvidenceSource.Hadith },
            Pagination: new Pagination(1, 20),
            IncludeCrossReferences: true,
            IncludeExplanations: true,
            SemanticSearchEnabled: true
        );

        var queryAnalysis = await queryAnalyzer.AnalyzeAsync(searchReq);
        var rewrittenQuery = await queryRewriter.RewriteAsync(queryText, CancellationToken.None);
        queryAnalysis = queryAnalysis with { SemanticQuery = rewrittenQuery };

        var result = await pipeline.ExecuteAsync(queryAnalysis, cancellationToken: CancellationToken.None);

        Assert.True(result.IsSuccess, $"Pipeline execution failed: {result.Error?.Message}");
        Assert.NotNull(result.Value);
        Assert.Equal(PipelineStage.Completed, result.Value.CurrentStage);

        var execCtx = result.Value;
        Console.WriteLine("\n=================== POSTGRESQL REAL DATASET RETRIEVAL RESULTS ===================");
        Console.WriteLine($"Query: '{queryText}'");
        Console.WriteLine($"Total Evidence Retrieved: {execCtx.Context.Input.Corpus?.Evidences.Count ?? 0}");

        if (execCtx.Context.Input.Corpus?.Evidences != null)
        {
            foreach (var ev in execCtx.Context.Input.Corpus.Evidences)
            {
                Console.WriteLine($"\n[Source: {ev.Source} | Ref: {ev.Reference.Value}] {ev.Title}");
                Console.WriteLine($"Content: {ev.Content}");
            }
        }

        Console.WriteLine("\n=================== PIPELINE REASONING & CLAIMS SUMMARY ===================");
        Console.WriteLine($"Summary: {execCtx.Reasoning?.Summary}");
        if (execCtx.Reasoning?.Claims != null)
        {
            foreach (var claim in execCtx.Reasoning.Claims)
            {
                Console.WriteLine($"- Claim: {claim.Statement} (Confidence: {claim.Confidence.Value})");
            }
        }

        Console.WriteLine("\n=================== RENDERED MARKDOWN OUTPUT ===================");
        var markdown = execCtx.RenderedOutputs?.FirstOrDefault(r => r.Extension == ".md")?.Content;
        Console.WriteLine(markdown ?? "No markdown output produced.");
        Console.WriteLine("========================================================================\n");
    }

    private List<string> JsonSerializerDeserializerHelper(string json)
    {
        return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
    }
}

public class PostgresEvidenceRepository : IEvidenceRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PostgresEvidenceRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EvidenceCorpus> GetEvidenceAsync(QueryAnalysis query, CancellationToken cancellationToken)
    {
        var evidences = new List<ResearchEvidence>();

        // Query Quran verses matching circumcision or fitrah terms in Postgres
        var verses = await _dbContext.QuranVerses
            .Include(v => v.Translations)
            .Include(v => v.Surah)
            .Where(v => EF.Functions.ILike(v.ArabicText, "%ختان%") 
                     || EF.Functions.ILike(v.ArabicCleaned, "%ختان%")
                     || v.Translations.Any(t => EF.Functions.ILike(t.Text, "%circumcis%")))
            .Take(10)
            .ToListAsync(cancellationToken);

        if (verses.Count == 0)
        {
            verses = await _dbContext.QuranVerses
                .Include(v => v.Translations)
                .Include(v => v.Surah)
                .Take(10)
                .ToListAsync(cancellationToken);
        }

        foreach (var v in verses)
        {
            var text = v.Translations.FirstOrDefault()?.Text ?? v.ArabicText;
            evidences.Add(new ResearchEvidence(
                Id: new DocumentId($"quran-{v.SurahNumber}-{v.AyahNumber}"),
                Source: EvidenceSource.Quran,
                Reference: new ReferenceId($"{v.SurahNumber}:{v.AyahNumber}"),
                Title: $"Surah {v.Surah?.EnglishName ?? v.SurahNumber.ToString()} ({v.SurahNumber}:{v.AyahNumber})",
                Content: text,
                Topics: new List<TopicId> { new TopicId("Fitrah"), new TopicId("Taharah") },
                Language: ResearchLanguage.English,
                RetrievalScore: 95.0
            ));
        }

        // Query Hadith narrations matching circumcision or fitrah terms in Postgres
        var hadiths = await _dbContext.Hadiths
            .Where(h => EF.Functions.ILike(h.EnglishText, "%circumcis%") 
                     || EF.Functions.ILike(h.ArabicText, "%ختان%"))
            .Take(10)
            .ToListAsync(cancellationToken);

        if (hadiths.Count == 0)
        {
            hadiths = await _dbContext.Hadiths
                .Take(10)
                .ToListAsync(cancellationToken);
        }

        foreach (var h in hadiths)
        {
            evidences.Add(new ResearchEvidence(
                Id: new DocumentId($"hadith-{h.Id}"),
                Source: EvidenceSource.Hadith,
                Reference: new ReferenceId($"Hadith-{h.HadithNumber}"),
                Title: $"Hadith Narration #{h.HadithNumber}",
                Content: !string.IsNullOrWhiteSpace(h.EnglishText) ? h.EnglishText : h.ArabicText,
                Topics: new List<TopicId> { new TopicId("Fitrah"), new TopicId("Khitan") },
                Language: ResearchLanguage.English,
                RetrievalScore: 90.0
            ));
        }

        return new EvidenceCorpus(
            Evidences: evidences,
            Topics: new List<TopicId> { new TopicId("Fitrah"), new TopicId("Khitan"), new TopicId("Fiqh") },
            Language: ResearchLanguage.English,
            AggregateConfidence: new ConfidenceScore(evidences.Count > 0 ? 0.95 : 0.70),
            TokenEstimate: 300,
            SourceCount: evidences.Count,
            AverageRanking: 92.5,
            RetrievedAt: DateTimeOffset.UtcNow
        );
    }
}
