using System;

namespace IslamicApp.Application.Research.Models;

public record EvidenceSnapshot(
    Guid Id,
    Guid ResearchSessionId,
    string EvidenceNodeId,
    double RetrievalScore,
    double Confidence,
    int Rank,
    string SnapshotHash,
    string DatasetVersion,
    string KnowledgeBaseVersion,
    string IndexerVersion,
    DateTimeOffset RetrievedAt
);
