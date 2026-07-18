using System;

namespace IslamicApp.Application.Research.Models;

public record DocumentRevision(
    Guid Id,
    Guid DocumentId,
    int RevisionNumber,
    Guid? ParentRevisionId,
    DateTimeOffset CreatedAt,
    string Summary,
    string Markdown,
    string Html,
    string Json,
    string DiffSummary,
    Guid? ReasoningSessionId,
    Guid? ExecutionSnapshotId,
    string GeneratedBy,
    string GenerationType
);
