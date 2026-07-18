using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Memory;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Persistence;
using IslamicApp.Infrastructure.Persistence.Entities;
using IslamicApp.Infrastructure.Persistence.Search;
using IslamicApp.Infrastructure.Research.Memory;
using IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

namespace IslamicApp.UnitTests;

public class ResearchMemoryAgentTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void ConfidenceCalculator_Should_Weighted_Aggregate_Dimensions()
    {
        // Arrange
        var calculator = new ConfidenceCalculator();
        var confidence = new CompositeConfidence(
            Evidence: 0.90,
            Citation: 0.80,
            Validation: 0.95,
            Reasoning: 0.85,
            Methodology: 1.0
        );

        // Action
        var result = calculator.Calculate(confidence);

        // Assert
        double expected = (0.90 * 0.35) + (0.80 * 0.25) + (0.95 * 0.20) + (0.85 * 0.15) + (1.0 * 0.05);
        Assert.Equal(expected, result.Score, 3);
        Assert.Contains("Evidence", result.Components.Keys);
        Assert.NotNull(result.Explanation);
    }

    [Fact]
    public void MemoryDecayStrategy_Should_Calculate_Temporal_Decay()
    {
        // Arrange
        var linear = new LinearDecayStrategy();
        var exponential = new ExponentialDecayStrategy();
        var workspaceSpecific = new WorkspaceSpecificDecayStrategy();
        
        var oldDate = DateTimeOffset.UtcNow.AddDays(-50);

        // Action
        var linearFactor = linear.GetDecayFactor(oldDate, "General");
        var expFactor = exponential.GetDecayFactor(oldDate, "General");
        var wsQuranFactor = workspaceSpecific.GetDecayFactor(oldDate, "Quranic");
        var wsOtherFactor = workspaceSpecific.GetDecayFactor(oldDate, "Legal");

        // Assert
        Assert.True(linearFactor < 1.0);
        Assert.True(expFactor < 1.0);
        Assert.Equal(1.0, wsQuranFactor); // Quranic does not decay
        Assert.True(wsOtherFactor < 1.0); // Other decays exponentially
    }

    [Fact]
    public async Task MemoryStore_Should_Save_Append_Only_History()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var store = new MemoryStore(dbContext);
        var entry = new MemoryEntry(
            WorkspaceId: Guid.NewGuid(),
            Query: "women prayer leadership",
            Summary: "Women leading prayer is allowed in female-only congregation.",
            Claims: new List<string> { "Assertion 1" },
            EvidenceIds: new List<string> { "node-1" },
            GraphNodes: new List<string> { "node-1" },
            EvidenceHash: "hash-value",
            Methodology: "Fiqh",
            Confidence: new CompositeConfidence(1.0, 1.0, 1.0, 1.0, 1.0),
            CreatedAt: DateTimeOffset.UtcNow,
            SchemaVersion: "v1",
            OriginSessionId: Guid.NewGuid(),
            OriginDocumentRevisionId: Guid.NewGuid(),
            CompressedFromVersion: "1.0.0",
            CreatedByModel: "Test",
            PromptVersion: "1.0.0"
        );

        // Action
        await store.StoreAsync(entry, CancellationToken.None);

        // Assert
        var memories = await store.GetWorkspaceMemoriesAsync(entry.WorkspaceId, CancellationToken.None);
        Assert.Single(memories);
        Assert.Equal("women prayer leadership", memories[0].Query);
    }

    [Fact]
    public async Task MemoryRanker_Should_Prioritize_Similar_Queries()
    {
        // Arrange
        var options = Options.Create(new MemoryRankingOptions
        {
            SemanticWeight = 0.45,
            CitationWeight = 0.25,
            MethodologyWeight = 0.15,
            RecencyWeight = 0.15
        });
        var ranker = new MemoryRanker(options, new NoDecayStrategy());

        var entries = new List<MemoryEntry>
        {
            CreateMockEntry("prayer rules", DateTimeOffset.UtcNow),
            CreateMockEntry("fasting principles", DateTimeOffset.UtcNow)
        };

        // Action
        var ranked = await ranker.RankAsync("prayer conditions", entries, CancellationToken.None);

        // Assert
        Assert.Equal("prayer rules", ranked[0].Query);
    }

    [Fact]
    public async Task MemoryContextBuilder_Should_Limit_Selection_To_Budget()
    {
        // Arrange
        var builder = new MemoryContextBuilder();
        var entries = new List<MemoryEntry>
        {
            CreateMockEntry("Query 1", DateTimeOffset.UtcNow),
            CreateMockEntry("Query 2", DateTimeOffset.UtcNow),
            CreateMockEntry("Query 3", DateTimeOffset.UtcNow)
        };

        // Action
        var selection = await builder.BuildContextAsync(entries, new MemoryContextOptions(10), CancellationToken.None);

        // Assert
        Assert.True(selection.Selected.Count < 3); // Rejected some due to tight budget
        Assert.NotEmpty(selection.RejectedEntries);
    }

    [Fact]
    public void IterationPlanner_Should_Generate_Retrieval_Plans_For_Gaps()
    {
        // Arrange
        var planner = new IterationPlanner(new ConfidenceCalculator());
        var context = new IterationContext(
            CurrentIteration: 0,
            State: PipelineState.Initial,
            Confidence: new CompositeConfidence(1.0, 1.0, 1.0, 1.0, 1.0),
            History: new List<IterationRecord>(),
            PendingGaps: new List<EvidenceGap>(),
            RetrievedEvidence: new List<string>()
        );

        var validationReport = new ValidationReport(
            ClaimValidation: new ClaimValidationReport(new List<ValidationIssue>
            {
                new ValidationIssue("ClaimValidation", "Missing claims details", ErrorSeverity.Error, new List<ReferenceId>())
            }),
            CitationValidation: new CitationValidationReport(new List<ValidationIssue>()),
            ConsistencyValidation: new ConsistencyValidationReport(new List<ValidationIssue>())
        );

        var reasoning = new ReasoningResult(
            Summary: "test summary",
            Claims: new List<ResearchClaim>
            {
                new ResearchClaim("women leading prayer", new List<ReferenceId>(), new ConfidenceScore(0.9), ClaimType.LegalRuling, ClaimOrigin.DirectEvidence)
            },
            Findings: new List<ResearchFinding>(),
            Limitations: new List<ResearchLimitation>(),
            Methodology: ResearchMethodologyType.Thematic,
            PromptVersion: "1.0",
            RawResponse: "raw response",
            Metadata: new GenerationMetadata("provider", "model", 100, 100, TimeSpan.FromSeconds(1), false, FinishReason.Stop)
        );

        var budget = new ReasoningBudget(3, 10000, TimeSpan.FromMinutes(2));

        // Action
        var decision = planner.Plan(context, validationReport, reasoning, budget);

        // Assert
        Assert.True(decision.Continue);
        Assert.Single(decision.Plans);
        Assert.Equal(KnowledgeGapType.MissingPrimaryEvidence, decision.Plans[0].Gap);
    }

    private MemoryEntry CreateMockEntry(string query, DateTimeOffset createdAt)
    {
        return new MemoryEntry(
            WorkspaceId: Guid.Empty,
            Query: query,
            Summary: $"Summary for {query}",
            Claims: new List<string>(),
            EvidenceIds: new List<string>(),
            GraphNodes: new List<string>(),
            EvidenceHash: "hash",
            Methodology: "Fiqh",
            Confidence: new CompositeConfidence(1.0, 1.0, 1.0, 1.0, 1.0),
            CreatedAt: createdAt,
            SchemaVersion: "v1",
            OriginSessionId: Guid.NewGuid(),
            OriginDocumentRevisionId: Guid.NewGuid(),
            CompressedFromVersion: "1.0",
            CreatedByModel: "Test",
            PromptVersion: "1.0"
        );
    }
}
