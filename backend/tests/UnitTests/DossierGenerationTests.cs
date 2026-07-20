using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Evaluation.Models;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Research.Dossier;
using IslamicApp.Application.Research.Analysis;

namespace IslamicApp.UnitTests;

public class DossierGenerationTests
{
    private readonly DossierGenerator _generator;

    public DossierGenerationTests()
    {
        _generator = new DossierGenerator();
    }

    [Fact]
    public async Task GenerateAsync_ValidExecutionContext_ProducesMarkdownAndStorageFile()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var refId = new ReferenceId("Hadith-5662");
        var meta = new GenerationMetadata("Model", "v1", 100, 100, TimeSpan.FromSeconds(1), false, FinishReason.Stop);
        var claim = new ResearchClaim("Circumcision is from Fitrah.", new List<ReferenceId> { refId }, new ConfidenceScore(0.95), ClaimType.Theological, ClaimOrigin.ModelInference);
        var reasoning = new ReasoningResult("Circumcision is established as Fitrah.", new List<ResearchClaim> { claim }, new List<ResearchFinding>(), new List<ResearchLimitation>(), ResearchMethodologyType.Thematic, "v1", "Raw", meta);

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
        var searchReq = new SearchRequest("What are the ruling about circumcision in Islam?", ResearchLanguage.English, new HashSet<EvidenceSource>(), new Pagination(1, 10), true, true, true);
        var analysis = new QueryAnalysis(searchReq, new NormalizedQuery("circumcision", "circumcision", new List<string>(), new List<string>(), new List<string>(), new List<string>()), ResearchLanguage.English, new QueryIntent(SearchMode.KeywordSearch, new HashSet<EvidenceSource>(), new HashSet<RetrievalCapability>(), 0.9), null, new List<string>());
        var input = new ResearchInput(analysis, corpus);
        var context = new ResearchContext(input);


        var session = new ReasoningSession(sessionId, new ResearchPrompt(new PromptTemplate("t", "v", "p", new Dictionary<string, string>()), new PromptVariables("", ResearchMethodologyType.Thematic, new List<EvidenceSnippet>(), new List<ReferenceId>()), "", ""), new GenerationResponse("{}", meta), meta, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        var execContext = new ResearchExecutionContext(context, ImmutableList<IDomainEvent>.Empty, PipelineStage.DossierGeneration, ImmutableList<PipelineStageExecution>.Empty)
            .WithReasoning(session, reasoning);

        var score = new ResearchQualityScore(0.95, 0.98, 0.90, 0.85, 0.94);
        var evaluation = new EvaluationResult(sessionId, score, new List<EvaluationFinding>(), "1.0.0", DateTimeOffset.UtcNow);

        // Act
        var result = await _generator.GenerateAsync(execContext, evaluation);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.MarkdownContent);
        Assert.NotEmpty(result.ContentHash);
        Assert.Contains("# Research Dossier", result.MarkdownContent);
        Assert.Contains("Circumcision", result.MarkdownContent);
        Assert.Contains("Overall Research Score", result.MarkdownContent);
        Assert.True(File.Exists(result.StoragePath));

        // Cleanup
        if (File.Exists(result.StoragePath))
        {
            File.Delete(result.StoragePath);
        }
    }
}
