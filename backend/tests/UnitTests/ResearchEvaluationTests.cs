using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Evaluation;
using IslamicApp.Application.Research.Evaluation.Models;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Research.Evaluation;
using IslamicApp.Application.Research.Analysis;

namespace IslamicApp.UnitTests;

public class ResearchEvaluationTests
{
    private readonly ICitationVerifier _verifier;

    public ResearchEvaluationTests()
    {
        _verifier = Substitute.For<ICitationVerifier>();
    }

    [Fact]
    public async Task EvaluateAsync_ValidClaimsAndEvidences_CalculatesWeightedScoreCorrectly()
    {
        // Arrange
        var verifierResult = new CitationVerificationResult(Exists: true, RelevanceScore: 1.0, Explanation: "Strong support");
        _verifier.VerifyAsync(Arg.Any<ReferenceId>(), Arg.Any<string>(), Arg.Any<EvidenceCorpus>())
            .Returns(Task.FromResult(verifierResult));

        var options = Options.Create(new EvaluationOptions());
        var engine = new ResearchEvaluationEngine(_verifier, options);

        var refId = new ReferenceId("Hadith-5662");
        var meta = new GenerationMetadata("Model", "v1", 100, 100, TimeSpan.FromSeconds(1), false, FinishReason.Stop);
        var claim = new ResearchClaim("Circumcision is a key practice of the Fitrah.", new List<ReferenceId> { refId }, new ConfidenceScore(0.95), ClaimType.Theological, ClaimOrigin.ModelInference);
        var reasoning = new ReasoningResult("Summary text", new List<ResearchClaim> { claim }, new List<ResearchFinding>(), new List<ResearchLimitation>(), ResearchMethodologyType.Thematic, "v1", "Raw", meta);

        var evidence = new ResearchEvidence(
            Id: new DocumentId("doc-1"),
            Source: EvidenceSource.Hadith,
            Reference: refId,
            Title: "Hadith 5662",
            Content: "Five practices are characteristics of the Fitra...",
            Topics: new List<TopicId>(),
            Language: ResearchLanguage.English,
            RetrievalScore: 90.0
        );
        var corpus = new EvidenceCorpus(new List<ResearchEvidence> { evidence }, new List<TopicId>(), ResearchLanguage.English, new ConfidenceScore(0.9), 100, 1, 90.0, DateTimeOffset.UtcNow);
        var searchReq = new SearchRequest("Query", ResearchLanguage.English, new HashSet<EvidenceSource>(), new Pagination(1, 10), true, true, true);
        var input = new ResearchInput(new QueryAnalysis(searchReq, new NormalizedQuery("query", "query", new List<string>(), new List<string>(), new List<string>(), new List<string>()), ResearchLanguage.English, new QueryIntent(SearchMode.KeywordSearch, new HashSet<EvidenceSource>(), new HashSet<RetrievalCapability>(), 0.9), null, new List<string>()), corpus);
        var context = new ResearchContext(input);


        var session = new ReasoningSession(Guid.NewGuid(), new ResearchPrompt(new PromptTemplate("t", "v", "p", new Dictionary<string, string>()), new PromptVariables("", ResearchMethodologyType.Thematic, new List<EvidenceSnippet>(), new List<ReferenceId>()), "", ""), new GenerationResponse("{}", meta), meta, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var execContext = new ResearchExecutionContext(context, ImmutableList<IDomainEvent>.Empty, PipelineStage.Evaluation, ImmutableList<PipelineStageExecution>.Empty)
            .WithReasoning(session, reasoning);

        // Act
        var evalResult = await engine.EvaluateAsync(execContext);

        // Assert
        Assert.NotNull(evalResult);
        Assert.True(evalResult.Score.OverallScore > 0.8);
        Assert.Equal(1.0, evalResult.Score.EvidenceCoverage);
        Assert.Equal(1.0, evalResult.Score.CitationAccuracy);
        Assert.Equal("1.0.0", evalResult.EvaluationVersion);
    }

    [Fact]
    public async Task EvaluateAsync_OverreachingClaim_EmitsWarningFinding()
    {
        // Arrange
        var verifierResult = new CitationVerificationResult(Exists: true, RelevanceScore: 1.0, Explanation: "Support");
        _verifier.VerifyAsync(Arg.Any<ReferenceId>(), Arg.Any<string>(), Arg.Any<EvidenceCorpus>())
            .Returns(Task.FromResult(verifierResult));

        var options = Options.Create(new EvaluationOptions());
        var engine = new ResearchEvaluationEngine(_verifier, options);

        var refId = new ReferenceId("Hadith-5662");
        var meta = new GenerationMetadata("Model", "v1", 100, 100, TimeSpan.FromSeconds(1), false, FinishReason.Stop);
        var claim = new ResearchClaim("All scholars unanimously agree circumcision is required.", new List<ReferenceId> { refId }, new ConfidenceScore(0.9), ClaimType.Theological, ClaimOrigin.ModelInference);
        var reasoning = new ReasoningResult("Summary text", new List<ResearchClaim> { claim }, new List<ResearchFinding>(), new List<ResearchLimitation>(), ResearchMethodologyType.Thematic, "v1", "Raw", meta);
        var corpus = new EvidenceCorpus(new List<ResearchEvidence>(), new List<TopicId>(), ResearchLanguage.English, new ConfidenceScore(0.9), 100, 0, 0.0, DateTimeOffset.UtcNow);
        var searchReq = new SearchRequest("Query", ResearchLanguage.English, new HashSet<EvidenceSource>(), new Pagination(1, 10), true, true, true);
        var input = new ResearchInput(new QueryAnalysis(searchReq, new NormalizedQuery("query", "query", new List<string>(), new List<string>(), new List<string>(), new List<string>()), ResearchLanguage.English, new QueryIntent(SearchMode.KeywordSearch, new HashSet<EvidenceSource>(), new HashSet<RetrievalCapability>(), 0.9), null, new List<string>()), corpus);
        var context = new ResearchContext(input);
        var session = new ReasoningSession(Guid.NewGuid(), new ResearchPrompt(new PromptTemplate("t", "v", "p", new Dictionary<string, string>()), new PromptVariables("", ResearchMethodologyType.Thematic, new List<EvidenceSnippet>(), new List<ReferenceId>()), "", ""), new GenerationResponse("{}", meta), meta, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var execContext = new ResearchExecutionContext(context, ImmutableList<IDomainEvent>.Empty, PipelineStage.Evaluation, ImmutableList<PipelineStageExecution>.Empty)
            .WithReasoning(session, reasoning);

        // Act
        var evalResult = await engine.EvaluateAsync(execContext);

        // Assert
        Assert.Contains(evalResult.Findings, f => f.Category == "ClaimOverreach" && f.Severity == ErrorSeverity.Warning);
    }
}
