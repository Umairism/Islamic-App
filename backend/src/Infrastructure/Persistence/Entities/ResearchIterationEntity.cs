using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class ResearchIterationEntity
{
    public Guid Id { get; set; }
    public Guid ResearchSessionId { get; set; }
    public int IterationNumber { get; set; }
    public string PipelineStage { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string GapsJson { get; set; } = "[]";
    public string RetrievedNodesJson { get; set; } = "[]";
    public string NewEvidenceJson { get; set; } = "[]";
    public double DurationMs { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation property
    public virtual ResearchSessionEntity? Session { get; set; }
}
