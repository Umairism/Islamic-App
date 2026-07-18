using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class MemoryEntryEntity
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Query { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ClaimsJson { get; set; } = "[]";
    public string EvidenceIdsJson { get; set; } = "[]";
    public string GraphNodesJson { get; set; } = "[]";
    public string EvidenceHash { get; set; } = string.Empty;
    public string Methodology { get; set; } = string.Empty;
    public double ConfidenceEvidence { get; set; }
    public double ConfidenceCitation { get; set; }
    public double ConfidenceValidation { get; set; }
    public double ConfidenceReasoning { get; set; }
    public double ConfidenceMethodology { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string SchemaVersion { get; set; } = "v1";
    public Guid OriginSessionId { get; set; }
    public Guid OriginDocumentRevisionId { get; set; }
    public string CompressedFromVersion { get; set; } = string.Empty;
    public string CreatedByModel { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public bool Invalidated { get; set; }
    public string? InvalidationReason { get; set; }
}
