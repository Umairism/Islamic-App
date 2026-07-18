using System;
using System.Collections.Generic;

namespace IslamicApp.Application.Research.Memory;

public enum ClaimProvenance
{
    DerivedFromMemory,
    DerivedFromEvidence,
    DerivedFromReasoning,
    DerivedFromIteration
}

public enum PipelineState
{
    Initial,
    Retrieving,
    Reasoning,
    Validating,
    GapDetected,
    AdditionalRetrieval,
    Completed,
    Failed
}

public enum KnowledgeGapType
{
    MissingPrimaryEvidence,
    WeakCitation,
    ContradictoryEvidence,
    LowConfidence,
    MissingHistoricalContext,
    MissingLinguisticEvidence,
    MissingConsensus
}

public enum MemoryInvalidationReason
{
    CorpusUpdated,
    CitationChanged,
    UserDeleted,
    WorkspaceArchived,
    Superseded,
    ManualReview
}

public enum IterationTerminationReason
{
    ConfidenceReached,
    MaxIterations,
    NoNewEvidence,
    ValidationFailure,
    CancellationRequested,
    Error
}

public enum SearchPriority
{
    Critical,
    High,
    Normal,
    Low
}

public enum SearchScope
{
    Global,
    Workspace,
    Local
}

public record CompositeConfidence(
    double Evidence,
    double Citation,
    double Validation,
    double Reasoning,
    double Methodology
);

public record EvidenceGap(
    KnowledgeGapType GapType,
    int Priority,
    string RetrievalStrategy,
    string SearchTerms,
    int MaxResults
);

public record ConfidenceResult(
    double Score,
    IReadOnlyDictionary<string, double> Components,
    string Explanation
);

public record RetrievalPlan(
    Guid PlanId,
    Guid CorrelationId,
    int Iteration,
    DateTimeOffset CreatedAt,
    KnowledgeGapType Gap,
    string Query,
    int MaxResults,
    SearchPriority Priority,
    SearchScope Scope
);

public record IterationRecord(
    int Iteration,
    IReadOnlyList<string> RetrievedNodes,
    IReadOnlyList<string> NewEvidence,
    ConfidenceResult ConfidenceResult,
    IReadOnlyList<EvidenceGap> KnowledgeGaps,
    TimeSpan Duration
);

public record IterationContext(
    int CurrentIteration,
    PipelineState State,
    CompositeConfidence Confidence,
    IReadOnlyList<IterationRecord> History,
    IReadOnlyList<EvidenceGap> PendingGaps,
    IReadOnlyList<string> RetrievedEvidence
);

public record ReasoningBudget(
    int RemainingIterations,
    int RemainingTokens,
    TimeSpan RemainingExecutionTime
);

public record ResearchExecutionContextMetadata(
    Guid CorrelationId,
    Guid SessionId,
    Guid WorkspaceId,
    Guid RequestId
);

public record ResearchExecutionMetrics(
    int RetrievalCount,
    int IterationCount,
    int EvidenceNodesRetrieved,
    int MemoryEntriesLoaded,
    int ValidationIssues,
    TimeSpan TotalDuration,
    int PromptTokens,
    int CompletionTokens
);

public record MemoryEntry(
    Guid WorkspaceId,
    string Query,
    string Summary,
    IReadOnlyList<string> Claims,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> GraphNodes,
    string EvidenceHash,
    string Methodology,
    CompositeConfidence Confidence,
    DateTimeOffset CreatedAt,
    string SchemaVersion,
    Guid OriginSessionId,
    Guid OriginDocumentRevisionId,
    string CompressedFromVersion,
    string CreatedByModel,
    string PromptVersion
);

public record MemorySelectionResult(
    IReadOnlyList<MemoryEntry> Selected,
    int TokensUsed,
    int TokensRemaining,
    IReadOnlyList<Guid> RejectedEntries,
    string SelectionReason
);

public record IterationDecision(
    bool Continue,
    IterationTerminationReason? Reason,
    IReadOnlyList<RetrievalPlan> Plans,
    ConfidenceResult Confidence
);

public record MemoryContextOptions(
    int TokenBudget
);

public class MemoryRankingOptions
{
    public double SemanticWeight { get; set; } = 0.45;
    public double CitationWeight { get; set; } = 0.25;
    public double MethodologyWeight { get; set; } = 0.15;
    public double RecencyWeight { get; set; } = 0.15;
}
