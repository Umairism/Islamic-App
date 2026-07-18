using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class DocumentRevisionEntity
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int RevisionNumber { get; set; }
    public Guid? ParentRevisionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
    public string DiffSummary { get; set; } = string.Empty;
    public Guid? ReasoningSessionId { get; set; }
    public Guid? ExecutionSnapshotId { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public string GenerationType { get; set; } = string.Empty;

    // Navigation properties
    public virtual ResearchDocumentEntity? Document { get; set; }
}
