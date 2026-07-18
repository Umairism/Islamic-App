using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;

using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Events;
using IslamicApp.Infrastructure.AI;
using IslamicApp.Infrastructure.AI.Providers;
using IslamicApp.Infrastructure.Research;
using IslamicApp.Infrastructure.Research.ValidationRules;
using IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;
using IslamicApp.Infrastructure.Research.Analysis.Methodologies;

namespace IslamicApp.UnitTests;

public class ResearchReasoningTests
{
    private readonly ResearchContext _context;
    private readonly GenerationMetadata _metadata;

    public ResearchReasoningTests()
    {
        // Set up context
        var query = new QueryAnalysis(
            new SearchRequest("wine", ResearchLanguage.English, new HashSet<EvidenceSource> { EvidenceSource.Quran }, new Pagination(1, 10), false, false, false),
            new NormalizedQuery("wine", "wine", new List<string>(), new List<string>(), new List<string>(), new List<string>()),
            ResearchLanguage.English,
            new QueryIntent(SearchMode.KeywordSearch, new HashSet<EvidenceSource> { EvidenceSource.Quran }, new HashSet<RetrievalCapability>(), 1.0),
            null,
            new List<string>()
        );

        var evidences = new List<ResearchEvidence>
        {
            new(
                Id: new DocumentId("quran-2-219"),
                Source: EvidenceSource.Quran,
                Reference: new ReferenceId("2:219"),
                Title: "Surah Al-Baqarah 219",
                Content: "They ask you about wine and gambling. Say: In them is great sin and yet some benefit...",
                Topics: new List<TopicId> { new("Intoxicants") },
                Language: ResearchLanguage.English,
                RetrievalScore: 95.0
            )
        };

        var corpus = new EvidenceCorpus(
            Evidences: evidences,
            Topics: new List<TopicId> { new("Intoxicants") },
            Language: ResearchLanguage.English,
            AggregateConfidence: new ConfidenceScore(0.95),
            TokenEstimate: 100,
            SourceCount: 1,
            AverageRanking: 1.0,
            RetrievedAt: DateTimeOffset.UtcNow
        );

        var graph = new EvidenceGraph(
            Nodes: new List<EvidenceNode>
            {
                new(new NodeId("node1"), new DocumentId("quran-2-219"), EvidenceClassification.PrimarySource, new ConfidenceScore(0.95))
            },
            Relationships: new List<EvidenceRelationship>()
        );

        var conflicts = new ConflictAnalysis(new List<EvidenceConflict>(), false, "No conflicts");
        var methodology = new ThematicMethodology();

        var analysis = new ResearchAnalysis(graph, conflicts, methodology);
        _context = new ResearchContext(new ResearchInput(query, corpus), analysis);

        _metadata = new GenerationMetadata(
            Provider: "MockProvider",
            Model: "MockModel",
            PromptTokens: 100,
            CompletionTokens: 100,
            Duration: TimeSpan.FromMilliseconds(200),
            Cached: false,
            FinishReason: FinishReason.Stop
        );
    }

    [Fact]
    public async Task PromptService_BuildsPromptWithExternalTemplatesOrFallbacks()
    {
        var service = new PromptService();
        var prompt = await service.BuildPromptAsync(_context, CancellationToken.None);

        Assert.NotNull(prompt);
        Assert.Equal("thematic", prompt.Template.Name);
        var combinedPrompt = prompt.RenderedSystemPrompt + " " + prompt.RenderedUserPrompt;
        Assert.Contains("wine", combinedPrompt);
        Assert.Contains("2:219", combinedPrompt);
    }

    [Fact]
    public void ReasoningParser_StripsMarkdownChatterAndMapsToDomainResult()
    {
        var rawJson = @"```json
        {
            ""summary"": ""Wine is prohibited gradually in Islam."",
            ""claims"": [
                {
                    ""statement"": ""Wine was warningly declared sinful."",
                    ""supportingEvidence"": [""2:219""],
                    ""confidence"": 0.90,
                    ""claimType"": ""LegalRuling"",
                    ""origin"": ""DirectEvidence""
                }
            ],
            ""findings"": [],
            ""limitations"": []
        }
        ```";

        var parser = new ReasoningParser();
        var parseResult = parser.Parse(rawJson, _context, _metadata);

        Assert.True(parseResult.IsSuccess);
        var value = parseResult.Value!;
        Assert.Equal("Wine is prohibited gradually in Islam.", value.Summary);
        Assert.Single(value.Claims);
        Assert.Equal("2:219", value.Claims[0].SupportingEvidence[0].Value);
        Assert.Equal(ClaimOrigin.DirectEvidence, value.Claims[0].Origin);
    }

    [Fact]
    public void CitationValidationRule_FlagsFabricatedCitations()
    {
        // 2:999 is fabricated because only 2:219 is in the corpus
        var reasoning = new ReasoningResult(
            Summary: "Summary text",
            Claims: new List<ResearchClaim>
            {
                new("Fabricated Claim", new List<ReferenceId> { new("2:999") }, new ConfidenceScore(0.95), ClaimType.Theological, ClaimOrigin.DirectEvidence)
            },
            Findings: new List<ResearchFinding>(),
            Limitations: new List<ResearchLimitation>(),
            Methodology: ResearchMethodologyType.Thematic,
            PromptVersion: "1.0.0",
            RawResponse: "{}",
            Metadata: _metadata
        );

        var rule = new CitationValidationRule();
        var issues = rule.Evaluate(reasoning, _context).ToList();

        Assert.Single(issues);
        Assert.Equal(ErrorSeverity.Error, issues[0].Severity);
        Assert.Contains("fabricated", issues[0].Description.ToLowerInvariant());
    }

    [Fact]
    public void ClaimValidationRule_FlagsLowConfidenceClaims()
    {
        var reasoning = new ReasoningResult(
            Summary: "Summary text",
            Claims: new List<ResearchClaim>
            {
                new("Low Confidence Claim", new List<ReferenceId> { new("2:219") }, new ConfidenceScore(0.20), ClaimType.Theological, ClaimOrigin.DirectEvidence)
            },
            Findings: new List<ResearchFinding>(),
            Limitations: new List<ResearchLimitation>(),
            Methodology: ResearchMethodologyType.Thematic,
            PromptVersion: "1.0.0",
            RawResponse: "{}",
            Metadata: _metadata
        );

        var rule = new ClaimValidationRule();
        var issues = rule.Evaluate(reasoning, _context).ToList();

        Assert.Single(issues);
        Assert.Equal(ErrorSeverity.Error, issues[0].Severity);
        Assert.Contains("confidence", issues[0].Description.ToLowerInvariant());
    }

    [Fact]
    public void ConsistencyValidationRule_FlagsContradictions()
    {
        var reasoning = new ReasoningResult(
            Summary: "Summary text",
            Claims: new List<ResearchClaim>
            {
                new("Intoxicants are prohibited.", new List<ReferenceId> { new("2:219") }, new ConfidenceScore(0.90), ClaimType.LegalRuling, ClaimOrigin.DirectEvidence),
                new("Intoxicants are permitted.", new List<ReferenceId> { new("2:219") }, new ConfidenceScore(0.90), ClaimType.LegalRuling, ClaimOrigin.DirectEvidence)
            },
            Findings: new List<ResearchFinding>(),
            Limitations: new List<ResearchLimitation>(),
            Methodology: ResearchMethodologyType.Thematic,
            PromptVersion: "1.0.0",
            RawResponse: "{}",
            Metadata: _metadata
        );

        var rule = new ConsistencyValidationRule();
        var issues = rule.Evaluate(reasoning, _context).ToList();

        Assert.Single(issues);
        Assert.Equal(ErrorSeverity.Warning, issues[0].Severity);
        Assert.Contains("contradiction", issues[0].Description.ToLowerInvariant());
    }

    [Fact]
    public void ExplainabilityBuilder_MapsSentencesToCanonicalNodePaths()
    {
        var reasoning = new ReasoningResult(
            Summary: "Surah Al-Baqarah discusses intoxicants.",
            Claims: new List<ResearchClaim>
            {
                new("Surah Al-Baqarah discusses intoxicants.", new List<ReferenceId> { new("2:219") }, new ConfidenceScore(0.90), ClaimType.Theological, ClaimOrigin.DirectEvidence)
            },
            Findings: new List<ResearchFinding>(),
            Limitations: new List<ResearchLimitation>(),
            Methodology: ResearchMethodologyType.Thematic,
            PromptVersion: "1.0.0",
            RawResponse: "{}",
            Metadata: _metadata
        );

        var builder = new ExplainabilityBuilder();
        var explainMap = builder.BuildMap(reasoning, _context);

        Assert.NotNull(explainMap);
        Assert.NotEmpty(explainMap.Traces);
        Assert.Single(explainMap.Traces[0].EvidencePath);
    }

    [Fact]
    public void OutputGuard_BlocksPublishabilityForCriticalErrors()
    {
        var validationReport = new ValidationReport(
            ClaimValidation: new ClaimValidationReport(new List<ValidationIssue> { new("LowConfidence", "Low confidence claim", ErrorSeverity.Error, new List<ReferenceId>()) }),
            CitationValidation: new CitationValidationReport(new List<ValidationIssue>()),
            ConsistencyValidation: new ConsistencyValidationReport(new List<ValidationIssue>())
        );

        var guard = new OutputGuard(new List<IResearchRenderer>());
        var execCtx = new ResearchExecutionContext(_context, ImmutableList<IDomainEvent>.Empty, PipelineStage.Completed, ImmutableList<PipelineStageExecution>.Empty);
        
        var reasoning = new ReasoningResult("Summary", new List<ResearchClaim>(), new List<ResearchFinding>(), new List<ResearchLimitation>(), ResearchMethodologyType.Thematic, "1.0.0", "{}", _metadata);
        var session = new ReasoningSession(Guid.NewGuid(), new ResearchPrompt(new PromptTemplate("t", "v", "p", new Dictionary<string, string>()), new PromptVariables("", ResearchMethodologyType.Thematic, new List<EvidenceSnippet>(), new List<ReferenceId>()), "", ""), new GenerationResponse("{}", _metadata), _metadata, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var publishResult = guard.EvaluatePublishability(execCtx, session, reasoning, validationReport, new ExplainabilityMap(new List<SourceTraceLink>()));

        Assert.False(publishResult.IsSuccess);
        Assert.Equal("PublishabilityBlocked", publishResult.Error!.Code);
    }

    [Fact]
    public async Task Renderers_FormatOutputAsynchronously()
    {
        var executionContext = new ResearchExecutionContext(
            Context: _context,
            Events: ImmutableList<IDomainEvent>.Empty,
            CurrentStage: PipelineStage.Completed,
            StageExecutions: ImmutableList<PipelineStageExecution>.Empty
        );

        var reasoning = new ReasoningResult("Summary synthesis text", new List<ResearchClaim>(), new List<ResearchFinding>(), new List<ResearchLimitation>(), ResearchMethodologyType.Thematic, "1.0.0", "{}", _metadata);
        var session = new ReasoningSession(Guid.NewGuid(), new ResearchPrompt(new PromptTemplate("t", "v", "p", new Dictionary<string, string>()), new PromptVariables("", ResearchMethodologyType.Thematic, new List<EvidenceSnippet>(), new List<ReferenceId>()), "", ""), new GenerationResponse("{}", _metadata), _metadata, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var resultEnvelope = new ResearchResult(
            ExecutionContext: executionContext,
            Session: session,
            Reasoning: reasoning,
            Validation: new ValidationReport(new ClaimValidationReport(new List<ValidationIssue>()), new CitationValidationReport(new List<ValidationIssue>()), new ConsistencyValidationReport(new List<ValidationIssue>())),
            Explainability: new ExplainabilityMap(new List<SourceTraceLink>()),
            Outputs: new List<RenderResult>()
        );

        var mdRenderer = new MarkdownRenderer();
        var htmlRenderer = new HtmlRenderer();

        var mdOutput = await mdRenderer.RenderAsync(resultEnvelope, CancellationToken.None);
        var htmlOutput = await htmlRenderer.RenderAsync(resultEnvelope, CancellationToken.None);

        Assert.Equal("text/markdown", mdOutput.ContentType);
        Assert.Contains("# Research Synthesis Report", mdOutput.Content);
        Assert.Equal("text/html", htmlOutput.ContentType);
        Assert.Contains("<html>", htmlOutput.Content);
    }

    [Fact]
    public async Task ResilientGenerationProviderDecorator_HandlesRetriesAndCircuitBreaks()
    {
        var telemetryMock = new MockTelemetry();
        var mockProvider = new FailMockProvider();

        var decorator = new ResilientGenerationProviderDecorator(mockProvider, telemetryMock);

        // Make 5 sequential calls to trigger the circuit breaker opening
        var prompt = new ResearchPrompt(new PromptTemplate("t", "v", "p", new Dictionary<string, string>()), new PromptVariables("", ResearchMethodologyType.Thematic, new List<EvidenceSnippet>(), new List<ReferenceId>()), "", "");
        var options = new GenerationOptions();

        for (int i = 0; i < 4; i++)
        {
            await Assert.ThrowsAnyAsync<Exception>(() => decorator.GenerateAsync(prompt, options, CancellationToken.None));
        }

        // The 5th call should open the circuit
        await Assert.ThrowsAnyAsync<Exception>(() => decorator.GenerateAsync(prompt, options, CancellationToken.None));

        // The 6th call should immediately fail-fast with Circuit Breaker Open exception
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => decorator.GenerateAsync(prompt, options, CancellationToken.None));
        Assert.Contains("circuit breaker is open", exception.Message.ToLowerInvariant());
    }

    private class MockTelemetry : IReasoningTelemetry
    {
        public void TrackUsage(GenerationMetadata metadata) {}
        public void TrackRetry(string provider, int attempt, Exception ex) {}
        public void TrackCircuitBreak(string provider, TimeSpan duration) {}
        public void TrackValidationFailure(ValidationReport report) {}
    }

    private class FailMockProvider : ITextGenerationProvider
    {
        public string ProviderName => "FailProvider";
        public bool SupportsJsonMode => true;
        public bool SupportsStreaming => false;
        public bool SupportsSeed => false;
        public bool SupportsVision => false;
        public bool SupportsTools => false;

        public Task<GenerationResponse> GenerateAsync(ResearchPrompt prompt, GenerationOptions options, CancellationToken cancellationToken)
        {
            throw new Exception("HTTP 503 Service Unavailable");
        }
    }
}
