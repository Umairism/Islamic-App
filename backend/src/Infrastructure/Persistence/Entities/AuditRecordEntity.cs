using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class AuditRecordEntity
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string OldStateHash { get; set; } = string.Empty;
    public string NewStateHash { get; set; } = string.Empty;
    public Guid CorrelationId { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}
