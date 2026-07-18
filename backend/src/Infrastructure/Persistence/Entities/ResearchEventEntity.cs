using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class ResearchEventEntity
{
    public Guid Id { get; set; }
    public Guid ResearchSessionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation property
    public virtual ResearchSessionEntity? Session { get; set; }
}
