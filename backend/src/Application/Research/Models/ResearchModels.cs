using System;
using System.Collections.Generic;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Memory;

namespace IslamicApp.Application.Research.Models;

public record ResearchEvidence(
    DocumentId Id,
    EvidenceSource Source,
    ReferenceId Reference,
    string Title,
    string Content,
    IReadOnlyList<TopicId> Topics,
    ResearchLanguage Language,
    double RetrievalScore
);

public record EvidenceCorpus(
    IReadOnlyList<ResearchEvidence> Evidences,
    IReadOnlyList<TopicId> Topics,
    ResearchLanguage Language,
    ConfidenceScore AggregateConfidence,
    int TokenEstimate,
    int SourceCount,
    double AverageRanking,
    DateTimeOffset RetrievedAt
);

public record ResearchInput(
    QueryAnalysis Query,
    EvidenceCorpus? Corpus = null
);

public enum EvidenceClassification
{
    PrimarySource,
    SecondarySource,
    Commentary,
    Definition,
    HistoricalContext,
    LegalPrinciple,
    Linguistic,
    CrossReference,
    Example
}


public record EvidenceRelationship(
    NodeId SourceNodeId,
    NodeId TargetNodeId,
    EvidenceRelationshipType Type,
    string Description
);

public record EvidenceNode(
    NodeId NodeId,
    DocumentId DocumentId,
    EvidenceClassification Classification,
    ConfidenceScore ConfidenceScore
);

public record EvidenceGraph(
    IReadOnlyList<EvidenceNode> Nodes,
    IReadOnlyList<EvidenceRelationship> Relationships
);

public enum ConflictType
{
    Contradiction,
    ApparentContradiction,
    WeakNarration,
    DifferentMadhhab,
    Abrogation,
    ContextDifference
}

public record EvidenceConflict(
    ReferenceId ReferenceA,
    ReferenceId ReferenceB,
    ConflictType Type,
    string Description,
    ConfidenceScore Confidence,
    string ResolutionGuidance
);

public record ConflictAnalysis(
    IReadOnlyList<EvidenceConflict> Conflicts,
    bool HasConflicts,
    string Summary
);

public record ResearchAnalysis(
    EvidenceGraph Graph,
    ConflictAnalysis Conflicts,
    IslamicApp.Application.Research.Analysis.IResearchMethodology Methodology
);

public record ResearchContext(
    ResearchInput Input,
    ResearchAnalysis? Analysis = null,
    MemorySelectionResult? Memory = null
)
{
    public ResearchContext WithCorpus(EvidenceCorpus corpus) =>
        this with { Input = Input with { Corpus = corpus } };

    public ResearchContext WithAnalysis(ResearchAnalysis analysis) =>
        this with { Analysis = analysis };
}
