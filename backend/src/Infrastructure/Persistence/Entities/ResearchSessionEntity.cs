using System;
using System.Collections.Generic;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class ResearchSessionEntity
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string Methodology { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public double ConfidenceValue { get; set; }
    public string ConfidenceLevel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CurrentStage { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Navigation properties
    public virtual WorkspaceEntity? Workspace { get; set; }
    public virtual ICollection<ResearchDocumentEntity> Documents { get; set; } = new List<ResearchDocumentEntity>();
    public virtual ICollection<EvidenceSnapshotEntity> EvidenceSnapshots { get; set; } = new List<EvidenceSnapshotEntity>();
    public virtual ICollection<ResearchExecutionSnapshotEntity> ExecutionSnapshots { get; set; } = new List<ResearchExecutionSnapshotEntity>();
    public virtual ICollection<ResearchSessionCollectionEntity> SessionCollections { get; set; } = new List<ResearchSessionCollectionEntity>();
    public virtual ICollection<ResearchIterationEntity> Iterations { get; set; } = new List<ResearchIterationEntity>();
    public virtual ICollection<ResearchEventEntity> Events { get; set; } = new List<ResearchEventEntity>();
    public virtual ICollection<ResearchResultEntity> Results { get; set; } = new List<ResearchResultEntity>();
}
