using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using IslamicApp.Application.Research.Evaluation.Models;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Research.Evaluation;
using IslamicApp.Application.Research.Enums;
using System;

namespace IslamicApp.UnitTests;

public class CitationVerificationTests
{
    private readonly CitationVerificationService _service;

    public CitationVerificationTests()
    {
        _service = new CitationVerificationService();
    }

    [Fact]
    public async Task VerifyAsync_ValidCitationAndRelevantContent_ReturnsExistsAndHighRelevance()
    {
        // Arrange
        var refId = new ReferenceId("Hadith-5662");
        var claimText = "Circumcision is a key practice of the Fitrah according to authentic Hadiths.";
        var evidences = new List<ResearchEvidence>
        {
            new ResearchEvidence(
                Id: new DocumentId("doc-1"),
                Source: EvidenceSource.Hadith,
                Reference: refId,
                Title: "Hadith Narration #5662",
                Content: "Circumcision is a key practice of the Fitrah according to authentic Hadiths.",
                Topics: new List<TopicId> { new TopicId("Fitrah") },
                Language: ResearchLanguage.English,
                RetrievalScore: 95.0
            )
        };
        var corpus = new EvidenceCorpus(evidences, new List<TopicId>(), ResearchLanguage.English, new ConfidenceScore(0.9), 100, 1, 95.0, DateTimeOffset.UtcNow);

        // Act
        var result = await _service.VerifyAsync(refId, claimText, corpus);

        // Assert
        Assert.True(result.Exists);
        Assert.True(result.RelevanceScore >= 0.8);
        Assert.Contains("strongly supports", result.Explanation);
    }

    [Fact]
    public async Task VerifyAsync_MissingCitationReference_ReturnsDoesNotExist()
    {
        // Arrange
        var refId = new ReferenceId("Hadith-99999");
        var claimText = "Circumcision is Fitrah.";
        var corpus = new EvidenceCorpus(new List<ResearchEvidence>(), new List<TopicId>(), ResearchLanguage.English, new ConfidenceScore(0.0), 0, 0, 0.0, DateTimeOffset.UtcNow);

        // Act
        var result = await _service.VerifyAsync(refId, claimText, corpus);

        // Assert
        Assert.False(result.Exists);
        Assert.Equal(0.0, result.RelevanceScore);
        Assert.Contains("empty", result.Explanation);
    }

    [Fact]
    public async Task VerifyAsync_ValidReferenceIrrelevantClaimText_ReturnsModerateRelevance()
    {
        // Arrange
        var refId = new ReferenceId("Hadith-5662");
        var claimText = "Circumcision is considered fitrah in Islam.";
        var evidences = new List<ResearchEvidence>
        {
            new ResearchEvidence(
                Id: new DocumentId("doc2"),
                Source: EvidenceSource.Hadith,
                Reference: new ReferenceId("Hadith-5662"),
                Title: "Hadith Narration #5662",
                Content: "Astronomical calculations for lunar moon sighting.",
                Topics: new List<TopicId>(),
                Language: ResearchLanguage.English,
                RetrievalScore: 100.0
            )
        };
        var corpus = new EvidenceCorpus(evidences, new List<TopicId>(), ResearchLanguage.English, new ConfidenceScore(0.9), 100, 1, 95.0, DateTimeOffset.UtcNow);

        // Act
        var result = await _service.VerifyAsync(refId, claimText, corpus);

        // Assert
        Assert.True(result.Exists);
        Assert.True(result.RelevanceScore >= 0.5);
    }
}
