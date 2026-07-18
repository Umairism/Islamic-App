using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class ResearchResultEntity
{
    public Guid Id { get; set; }
    public Guid ResearchSessionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string CitationsJson { get; set; } = "[]";
    public int Version { get; set; }
    public bool IsFinal { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }

    // Navigation property
    public virtual ResearchSessionEntity? Session { get; set; }
}
