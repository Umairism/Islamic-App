using System;

namespace IslamicApp.Application.Research.Models;

public record AuditRecord(
    Guid Id,
    string Action,
    string Actor,
    string EntityType,
    string EntityId,
    string OldStateHash,
    string NewStateHash,
    Guid CorrelationId,
    string RequestId,
    string UserId,
    string MachineName,
    string ApplicationVersion,
    DateTimeOffset OccurredAt
);
