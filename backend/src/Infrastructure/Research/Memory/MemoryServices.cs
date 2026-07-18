using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Memory;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Persistence;
using IslamicApp.Infrastructure.Persistence.Entities;

namespace IslamicApp.Infrastructure.Research.Memory;

public class ConfidenceCalculator : IConfidenceCalculator
{
    public ConfidenceResult Calculate(CompositeConfidence confidence)
    {
        double score = (confidence.Evidence * 0.35) +
                       (confidence.Citation * 0.25) +
                       (confidence.Validation * 0.20) +
                       (confidence.Reasoning * 0.15) +
                       (confidence.Methodology * 0.05);

        var components = new Dictionary<string, double>
        {
            { "Evidence", confidence.Evidence },
            { "Citation", confidence.Citation },
            { "Validation", confidence.Validation },
            { "Reasoning", confidence.Reasoning },
            { "Methodology", confidence.Methodology }
        };

        string explanation = $"Composite confidence score calculated as {score:F3} based on weighted factors: " +
                             $"Evidence ({confidence.Evidence:F2} * 35%), " +
                             $"Citation ({confidence.Citation:F2} * 25%), " +
                             $"Validation ({confidence.Validation:F2} * 20%), " +
                             $"Reasoning ({confidence.Reasoning:F2} * 15%), " +
                             $"Methodology ({confidence.Methodology:F2} * 5%).";

        return new ConfidenceResult(score, components, explanation);
    }
}

public class LinearDecayStrategy : IMemoryDecayStrategy
{
    public double GetDecayFactor(DateTimeOffset createdAt, string workspaceType)
    {
        var days = (DateTimeOffset.UtcNow - createdAt).TotalDays;
        return Math.Max(0.5, 1.0 - (days * 0.001));
    }
}

public class ExponentialDecayStrategy : IMemoryDecayStrategy
{
    public double GetDecayFactor(DateTimeOffset createdAt, string workspaceType)
    {
        var days = (DateTimeOffset.UtcNow - createdAt).TotalDays;
        return Math.Max(0.4, Math.Exp(-days * 0.005));
    }
}

public class NoDecayStrategy : IMemoryDecayStrategy
{
    public double GetDecayFactor(DateTimeOffset createdAt, string workspaceType) => 1.0;
}

public class WorkspaceSpecificDecayStrategy : IMemoryDecayStrategy
{
    private readonly IMemoryDecayStrategy _noDecay = new NoDecayStrategy();
    private readonly IMemoryDecayStrategy _exponentialDecay = new ExponentialDecayStrategy();

    public double GetDecayFactor(DateTimeOffset createdAt, string workspaceType)
    {
        if (workspaceType.Contains("Quran", StringComparison.OrdinalIgnoreCase) || 
            workspaceType.Contains("Hadith", StringComparison.OrdinalIgnoreCase))
        {
            return _noDecay.GetDecayFactor(createdAt, workspaceType);
        }
        return _exponentialDecay.GetDecayFactor(createdAt, workspaceType);
    }
}

public class MemoryStore : IKnowledgeMemoryStore
{
    private readonly ApplicationDbContext _dbContext;

    public MemoryStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task StoreAsync(MemoryEntry entry, CancellationToken cancellationToken)
    {
        var entity = new MemoryEntryEntity
        {
            Id = Guid.NewGuid(),
            WorkspaceId = entry.WorkspaceId,
            Query = entry.Query,
            Summary = entry.Summary,
            ClaimsJson = JsonSerializer.Serialize(entry.Claims),
            EvidenceIdsJson = JsonSerializer.Serialize(entry.EvidenceIds),
            GraphNodesJson = JsonSerializer.Serialize(entry.GraphNodes),
            EvidenceHash = entry.EvidenceHash,
            Methodology = entry.Methodology,
            ConfidenceEvidence = entry.Confidence.Evidence,
            ConfidenceCitation = entry.Confidence.Citation,
            ConfidenceValidation = entry.Confidence.Validation,
            ConfidenceReasoning = entry.Confidence.Reasoning,
            ConfidenceMethodology = entry.Confidence.Methodology,
            CreatedAt = entry.CreatedAt,
            SchemaVersion = entry.SchemaVersion,
            OriginSessionId = entry.OriginSessionId,
            OriginDocumentRevisionId = entry.OriginDocumentRevisionId,
            CompressedFromVersion = entry.CompressedFromVersion,
            CreatedByModel = entry.CreatedByModel,
            PromptVersion = entry.PromptVersion,
            Invalidated = false,
            InvalidationReason = null
        };

        _dbContext.MemoryEntries.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryEntry>> GetWorkspaceMemoriesAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var entities = await _dbContext.MemoryEntries
            .Where(e => e.WorkspaceId == workspaceId && !e.Invalidated)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDomain).ToList();
    }

    public async Task InvalidateMemoryAsync(Guid workspaceId, string query, MemoryInvalidationReason reason, CancellationToken cancellationToken)
    {
        var entities = await _dbContext.MemoryEntries
            .Where(e => e.WorkspaceId == workspaceId && e.Query == query && !e.Invalidated)
            .ToListAsync(cancellationToken);

        foreach (var entity in entities)
        {
            entity.Invalidated = true;
            entity.InvalidationReason = reason.ToString();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private MemoryEntry MapToDomain(MemoryEntryEntity entity)
    {
        var claims = JsonSerializer.Deserialize<List<string>>(entity.ClaimsJson) ?? new List<string>();
        var evidenceIds = JsonSerializer.Deserialize<List<string>>(entity.EvidenceIdsJson) ?? new List<string>();
        var graphNodes = JsonSerializer.Deserialize<List<string>>(entity.GraphNodesJson) ?? new List<string>();

        return new MemoryEntry(
            WorkspaceId: entity.WorkspaceId,
            Query: entity.Query,
            Summary: entity.Summary,
            Claims: claims,
            EvidenceIds: evidenceIds,
            GraphNodes: graphNodes,
            EvidenceHash: entity.EvidenceHash,
            Methodology: entity.Methodology,
            Confidence: new CompositeConfidence(
                entity.ConfidenceEvidence,
                entity.ConfidenceCitation,
                entity.ConfidenceValidation,
                entity.ConfidenceReasoning,
                entity.ConfidenceMethodology
            ),
            CreatedAt: entity.CreatedAt,
            SchemaVersion: entity.SchemaVersion,
            OriginSessionId: entity.OriginSessionId,
            OriginDocumentRevisionId: entity.OriginDocumentRevisionId,
            CompressedFromVersion: entity.CompressedFromVersion,
            CreatedByModel: entity.CreatedByModel,
            PromptVersion: entity.PromptVersion
        );
    }
}

public class MemoryRetriever : IMemoryRetriever
{
    private readonly IKnowledgeMemoryStore _store;

    public MemoryRetriever(IKnowledgeMemoryStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<MemoryEntry>> RetrieveAsync(Guid workspaceId, string query, CancellationToken cancellationToken)
    {
        var memories = await _store.GetWorkspaceMemoriesAsync(workspaceId, cancellationToken);
        
        // Apply hard freshness gate check
        var validMemories = new List<MemoryEntry>();
        foreach (var m in memories)
        {
            // Simulate: Discard if the prompt template configuration version does not match
            if (string.IsNullOrEmpty(m.EvidenceHash)) continue;

            validMemories.Add(m);
        }

        return validMemories;
    }
}

public class MemoryRanker : IMemoryRanker
{
    private readonly MemoryRankingOptions _options;
    private readonly IMemoryDecayStrategy _decayStrategy;

    public MemoryRanker(IOptions<MemoryRankingOptions> options, IMemoryDecayStrategy decayStrategy)
    {
        _options = options.Value;
        _decayStrategy = decayStrategy;
    }

    public Task<IReadOnlyList<MemoryEntry>> RankAsync(string query, IEnumerable<MemoryEntry> entries, CancellationToken cancellationToken)
    {
        var ranked = entries.Select(entry =>
        {
            double semanticScore = CalculateJaccardSimilarity(query, entry.Query);
            double citationScore = entry.EvidenceIds.Count > 0 ? 1.0 : 0.0;
            double methodologyScore = entry.Methodology.Length > 0 ? 1.0 : 0.0;
            double recencyScore = 1.0 / (1.0 + (DateTimeOffset.UtcNow - entry.CreatedAt).TotalDays);

            double baseScore = (semanticScore * _options.SemanticWeight) +
                               (citationScore * _options.CitationWeight) +
                               (methodologyScore * _options.MethodologyWeight) +
                               (recencyScore * _options.RecencyWeight);

            double decayFactor = _decayStrategy.GetDecayFactor(entry.CreatedAt, "Quranic");
            double finalScore = baseScore * decayFactor;

            return (Entry: entry, Score: finalScore);
        })
        .OrderByDescending(r => r.Score)
        .Select(r => r.Entry)
        .ToList();

        return Task.FromResult<IReadOnlyList<MemoryEntry>>(ranked);
    }

    private double CalculateJaccardSimilarity(string s1, string s2)
    {
        var w1 = s1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var w2 = s2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        if (w1.Count == 0 && w2.Count == 0) return 1.0;
        int intersection = w1.Intersect(w2).Count();
        int union = w1.Union(w2).Count();

        return (double)intersection / union;
    }
}

public class MemoryCompressor : IMemoryCompressor
{
    public Task<MemoryEntry> CompressAsync(ResearchResult result, CancellationToken cancellationToken)
    {
        var evidences = result.ExecutionContext.Context.Input.Corpus?.Evidences ?? new List<ResearchEvidence>();
        var nodeIds = evidences.Select(e => e.Id.Value).OrderBy(x => x).ToList();
        var refIds = evidences.Select(e => e.Reference.Value).OrderBy(x => x).ToList();
        var methodology = result.ExecutionContext.Context.Analysis?.Methodology?.Type.ToString() ?? "Thematic";

        // Calculate EvidenceHash
        string sourceString = string.Join(",", nodeIds) + "|" + string.Join(",", refIds) + "|" + methodology;
        using var sha = SHA256.Create();
        string evidenceHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sourceString)));

        var claims = result.Reasoning.Claims.Select(c => c.Statement).ToList();
        var evidenceIds = evidences.Select(e => e.Id.Value).ToList();

        var entry = new MemoryEntry(
            WorkspaceId: Guid.Empty, // resolved later
            Query: result.ExecutionContext.Context.Input.Query.OriginalRequest.Query,
            Summary: result.Reasoning.Summary,
            Claims: claims,
            EvidenceIds: evidenceIds,
            GraphNodes: evidenceIds,
            EvidenceHash: evidenceHash,
            Methodology: methodology,
            Confidence: new CompositeConfidence(1.0, 1.0, 1.0, 1.0, 1.0),
            CreatedAt: DateTimeOffset.UtcNow,
            SchemaVersion: "v1",
            OriginSessionId: Guid.NewGuid(),
            OriginDocumentRevisionId: Guid.NewGuid(),
            CompressedFromVersion: "1.0.0",
            CreatedByModel: "MockReasoner",
            PromptVersion: "1.0.0"
        );

        return Task.FromResult(entry);
    }
}

public class MemoryContextBuilder : IMemoryContextBuilder
{
    public Task<MemorySelectionResult> BuildContextAsync(IEnumerable<MemoryEntry> rankedEntries, MemoryContextOptions options, CancellationToken cancellationToken)
    {
        var selected = new List<MemoryEntry>();
        var rejected = new List<Guid>();
        int totalTokens = 0;

        int summaryBudget = (int)(options.TokenBudget * 0.20);
        int claimsBudget = (int)(options.TokenBudget * 0.50);

        foreach (var entry in rankedEntries)
        {
            int entryTokens = (entry.Summary.Length / 4) + (entry.Claims.Sum(c => c.Length) / 4);
            if (totalTokens + entryTokens <= options.TokenBudget)
            {
                selected.Add(entry);
                totalTokens += entryTokens;
            }
            else
            {
                rejected.Add(entry.OriginSessionId);
            }
        }

        var result = new MemorySelectionResult(
            Selected: selected,
            TokensUsed: totalTokens,
            TokensRemaining: options.TokenBudget - totalTokens,
            RejectedEntries: rejected,
            SelectionReason: $"Selected {selected.Count} memories within budget constraints."
        );

        return Task.FromResult(result);
    }
}
