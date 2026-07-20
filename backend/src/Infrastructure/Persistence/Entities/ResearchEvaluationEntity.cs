using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class ResearchEvaluationEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ResearchSessionId { get; set; }

    public double OverallScore { get; set; }

    public double EvidenceCoverage { get; set; }

    public double CitationAccuracy { get; set; }

    public double ReasoningConsistency { get; set; }

    public double SourceDiversity { get; set; }

    public string MetricsJson { get; set; } = "{}";

    public string FindingsJson { get; set; } = "[]";

    public string EvaluationVersion { get; set; } = "1.0.0";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
