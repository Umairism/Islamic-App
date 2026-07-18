using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class EvidenceSnapshotEntity
{
    public Guid Id { get; set; }
    public Guid ResearchSessionId { get; set; }
    public string EvidenceNodeId { get; set; } = string.Empty;
    public double RetrievalScore { get; set; }
    public double Confidence { get; set; }
    public int Rank { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
    public string DatasetVersion { get; set; } = string.Empty;
    public string KnowledgeBaseVersion { get; set; } = string.Empty;
    public string IndexerVersion { get; set; } = string.Empty;
    public DateTimeOffset RetrievedAt { get; set; }

    // Navigation properties
    public virtual ResearchSessionEntity? Session { get; set; }
}
