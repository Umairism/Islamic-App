using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Infrastructure.Research.Analysis;
using IslamicApp.Infrastructure.Research.Analysis.ConflictRules;
using IslamicApp.Infrastructure.Research.Analysis.Methodologies;
using IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;
using Microsoft.Extensions.Logging;

namespace IslamicApp.UnitTests;

public class ResearchCoreTests
{
    [Fact]
    public void ResultMonad_SuccessAndFailure_BehaveCorrectly()
    {
        // Success case
        var successResult = Result<string>.Success("Success Value");
        Assert.True(successResult.IsSuccess);
        Assert.Equal("Success Value", successResult.Value);
        Assert.Null(successResult.Error);

        // Failure case
        var error = new Error("TestCode", "Test Message", ErrorSeverity.Error);
        var failureResult = Result<string>.Failure(error);
        Assert.False(failureResult.IsSuccess);
        Assert.Null(failureResult.Value);
        Assert.NotNull(failureResult.Error);
        Assert.Equal("TestCode", failureResult.Error.Code);
        Assert.Equal("Test Message", failureResult.Error.Message);
        Assert.Equal(ErrorSeverity.Error, failureResult.Error.Severity);
    }

    [Theory]
    [InlineData(0.2, ConfidenceLevel.Low)]
    [InlineData(0.5, ConfidenceLevel.Medium)]
    [InlineData(0.85, ConfidenceLevel.High)]
    [InlineData(0.95, ConfidenceLevel.VeryHigh)]
    public void ConfidenceScore_DerivesLevelFromValue(double value, ConfidenceLevel expectedLevel)
    {
        var score = new ConfidenceScore(value);
        Assert.Equal(value, score.Value);
        Assert.Equal(expectedLevel, score.Level);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void ConfidenceScore_ThrowsException_IfValueOutOfBounds(double invalidValue)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConfidenceScore(invalidValue));
    }

    [Fact]
    public void PluggableMethodologies_OrderEvidenceAndReturnSections()
    {
        var evidence = new List<ResearchEvidence>
        {
            new(new DocumentId("doc1"), EvidenceSource.Quran, new ReferenceId("2:255"), "Title1", "Content1", new List<TopicId> { new("Ethics") }, ResearchLanguage.English, 75.0),
            new(new DocumentId("doc2"), EvidenceSource.Hadith, new ReferenceId("3:10"), "Title2", "Content2", new List<TopicId> { new("Family") }, ResearchLanguage.English, 90.0)
        };

        // Fiqh Methodology places Quran first
        var fiqh = new FiqhMethodology();
        var orderedFiqh = fiqh.OrderEvidence(evidence);
        Assert.Equal(EvidenceSource.Quran, orderedFiqh[0].Source);

        var fiqhSections = fiqh.GetRequiredOutputSections();
        Assert.Contains("Legal Proofs (Adillah)", fiqhSections);

        // Chronological Methodology sorts by reference value string
        var chrono = new ChronologicalMethodology();
        var orderedChrono = chrono.OrderEvidence(evidence);
        Assert.Equal("2:255", orderedChrono[0].Reference.Value);
    }

    [Fact]
    public void EvidenceDeduplicator_GroupsSameReferencesAndConsolidates()
    {
        var evidence = new List<ResearchEvidence>
        {
            new(new DocumentId("doc1"), EvidenceSource.Quran, new ReferenceId("2:255"), "Qur'an 2:255 Edition A", "Content A", new List<TopicId> { new("Ethics") }, ResearchLanguage.English, 75.0),
            new(new DocumentId("doc2"), EvidenceSource.Quran, new ReferenceId("2:255"), "Qur'an 2:255 Edition B", "Content B", new List<TopicId> { new("Worship") }, ResearchLanguage.English, 95.0)
        };

        var corpus = new EvidenceCorpus(
            Evidences: evidence,
            Topics: new List<TopicId>(),
            Language: ResearchLanguage.English,
            AggregateConfidence: new ConfidenceScore(0.85),
            TokenEstimate: 100,
            SourceCount: 1,
            AverageRanking: 85.0,
            RetrievedAt: DateTimeOffset.UtcNow
        );

        var deduplicator = new EvidenceDeduplicator();
        var resultCorpus = deduplicator.Deduplicate(corpus);

        // Should merge duplicates of 2:255 into 1 representative
        Assert.Single(resultCorpus.Evidences);
        var representative = resultCorpus.Evidences.First();
        Assert.Equal("Qur'an 2:255 Edition B", representative.Title); // High score edition
        Assert.Contains(representative.Topics, t => t.Value == "Ethics");
        Assert.Contains(representative.Topics, t => t.Value == "Worship");
    }

    [Fact]
    public void GraphBuilder_ConstructsDeterministicNodesAndEdges()
    {
        var evidence = new List<ResearchEvidence>
        {
            new(new DocumentId("quran-2-255"), EvidenceSource.Quran, new ReferenceId("2:255"), "Ayat Al Kursi", "Allah! There is no deity except Him...", new List<TopicId> { new("Tawhid") }, ResearchLanguage.English, 100.0),
            new(new DocumentId("hadith-1"), EvidenceSource.Hadith, new ReferenceId("Hadith 5971"), "Virtues of Ayat Al Kursi", "He who recites 2:255 after every prayer...", new List<TopicId> { new("Tawhid") }, ResearchLanguage.English, 90.0)
        };

        var corpus = new EvidenceCorpus(
            Evidences: evidence,
            Topics: new List<TopicId> { new("Tawhid") },
            Language: ResearchLanguage.English,
            AggregateConfidence: new ConfidenceScore(0.95),
            TokenEstimate: 100,
            SourceCount: 2,
            AverageRanking: 95.0,
            RetrievedAt: DateTimeOffset.UtcNow
        );

        var analyzer = new EvidenceAnalyzer();
        var graphBuilder = new GraphBuilder(analyzer);
        var graph = graphBuilder.BuildGraph(corpus, new QueryAnalysis(
            new SearchRequest("", ResearchLanguage.English, new HashSet<EvidenceSource>(), new Pagination(1, 10), false, false, false),
            new NormalizedQuery("", "", new List<string>(), new List<string>(), new List<string>(), new List<string>()),
            ResearchLanguage.English,
            new QueryIntent(SearchMode.KeywordSearch, new HashSet<EvidenceSource>(), new HashSet<RetrievalCapability>(), 1.0),
            null,
            new List<string>()
        ));

        Assert.Equal(2, graph.Nodes.Count);
        Assert.Contains(graph.Nodes, n => n.Classification == EvidenceClassification.PrimarySource); // Quran
        Assert.Contains(graph.Nodes, n => n.Classification == EvidenceClassification.SecondarySource); // Hadith

        // Deteministic node IDs should be generated
        Assert.All(graph.Nodes, n => Assert.False(string.IsNullOrWhiteSpace(n.NodeId.Value)));

        // Relationship should exist based on cross-reference or shared topics
        Assert.NotEmpty(graph.Relationships);
    }

    [Fact]
    public void ConflictDetector_FlagsWeakNarrationsAndMadhhabDifferences()
    {
        var rules = new List<IConflictRule>
        {
            new WeakNarrationRule(),
            new MadhhabDifferenceRule()
        };
        var detector = new ConflictDetector(rules);

        var nodes = new List<EvidenceNode>
        {
            new(new NodeId("n1"), new DocumentId("weak-hadith"), EvidenceClassification.SecondarySource, new ConfidenceScore(0.2)) // Low confidence triggers rule
        };
        var graph = new EvidenceGraph(nodes, new List<EvidenceRelationship>());

        // Mock a query analysis looking for fiqh to trigger madhhab rule
        var query = new QueryAnalysis(
            new SearchRequest("", ResearchLanguage.English, new HashSet<EvidenceSource>(), new Pagination(1, 10), false, false, false),
            new NormalizedQuery("", "wudu ruling details", new List<string>(), new List<string>(), new List<string>(), new List<string>()),
            ResearchLanguage.English,
            new QueryIntent(SearchMode.KeywordSearch, new HashSet<EvidenceSource>(), new HashSet<RetrievalCapability>(), 1.0),
            null,
            new List<string>()
        );

        var analysis = detector.DetectConflicts(graph, query);

        Assert.True(analysis.HasConflicts);
        Assert.Equal(2, analysis.Conflicts.Count);
        Assert.Contains(analysis.Conflicts, c => c.Type == ConflictType.WeakNarration);
        Assert.Contains(analysis.Conflicts, c => c.Type == ConflictType.DifferentMadhhab);
    }

    [Fact]
    public async Task ResearchPipeline_ExecutesChainBehaviorsSuccessfully()
    {
        // 1. Setup mock repository returning fake corpus
        var retrievedAt = DateTimeOffset.UtcNow;
        var fakeCorpus = new EvidenceCorpus(
            Evidences: new List<ResearchEvidence>
            {
                new(new DocumentId("doc1"), EvidenceSource.Quran, new ReferenceId("2:255"), "Title1", "Content1", new List<TopicId> { new("Ethics") }, ResearchLanguage.English, 90.0)
            },
            Topics: new List<TopicId> { new("Ethics") },
            Language: ResearchLanguage.English,
            AggregateConfidence: new ConfidenceScore(0.90),
            TokenEstimate: 50,
            SourceCount: 1,
            AverageRanking: 90.0,
            RetrievedAt: retrievedAt
        );

        var mockRepo = new MockEvidenceRepository(fakeCorpus);

        // 2. Setup analyzer and selectors
        var analyzer = new EvidenceAnalyzer();
        var deduplicator = new EvidenceDeduplicator();
        var graphBuilder = new GraphBuilder(analyzer);
        var conflictDetector = new ConflictDetector(new List<IConflictRule> { new WeakNarrationRule() });
        var methodologySelector = new MethodologySelector();

        var thematic = new ThematicMethodology();
        var mockFactory = new MockMethodologyFactory(thematic);

        var analysisBuilder = new ResearchAnalysisBuilder(methodologySelector, mockFactory, graphBuilder, conflictDetector);

        // 3. Chain pipeline behaviors
        var behaviors = new List<IResearchPipelineBehavior>
        {
            new ExceptionBehavior(new MockLogger<ExceptionBehavior>()),
            new LoggingBehavior(new MockLogger<LoggingBehavior>()),
            new RetrievalBehavior(mockRepo),
            new DeduplicationBehavior(deduplicator),
            new AnalysisBehavior(analysisBuilder)
        };

        var pipeline = new ResearchPipeline(behaviors);

        var query = new QueryAnalysis(
            new SearchRequest("test query", ResearchLanguage.English, new HashSet<EvidenceSource>(), new Pagination(1, 10), false, false, false),
            new NormalizedQuery("test query", "test query", new List<string>(), new List<string>(), new List<string>(), new List<string>()),
            ResearchLanguage.English,
            new QueryIntent(SearchMode.KeywordSearch, new HashSet<EvidenceSource>(), new HashSet<RetrievalCapability>(), 1.0),
            null,
            new List<string>()
        );

        var result = await pipeline.ExecuteAsync(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var execContext = result.Value;
        Assert.NotNull(execContext);
        Assert.Equal(PipelineStage.Completed, execContext.CurrentStage);
        
        // Assert domain events were correctly raised during the pipeline process
        Assert.NotEmpty(execContext.Events);
        Assert.Contains(execContext.Events, e => e is EvidenceDeduplicatedEvent);
        Assert.Contains(execContext.Events, e => e is GraphBuiltEvent);
        Assert.Contains(execContext.Events, e => e is ConflictDetectedEvent);
        Assert.Contains(execContext.Events, e => e is MethodologySelectedEvent);

        // Assert ResearchAnalysis is fully populated
        Assert.NotNull(execContext.Context.Analysis);
        Assert.Equal(ResearchMethodologyType.Thematic, execContext.Context.Analysis.Methodology.Type);
        Assert.Single(execContext.Context.Analysis.Graph.Nodes);
    }
}

public class MockEvidenceRepository : IEvidenceRepository
{
    private readonly EvidenceCorpus _corpus;

    public MockEvidenceRepository(EvidenceCorpus corpus)
    {
        _corpus = corpus;
    }

    public Task<EvidenceCorpus> GetEvidenceAsync(QueryAnalysis query, CancellationToken cancellationToken)
    {
        return Task.FromResult(_corpus);
    }
}

public class MockMethodologyFactory : IResearchMethodologyFactory
{
    private readonly IResearchMethodology _methodology;

    public MockMethodologyFactory(IResearchMethodology methodology)
    {
        _methodology = methodology;
    }

    public IResearchMethodology CreateMethodology(ResearchMethodologyType type)
    {
        return _methodology;
    }
}

public class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
