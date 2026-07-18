using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class ResearchExecutionSnapshotEntity
{
    public Guid Id { get; set; }
    public Guid ResearchSessionId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PromptHash { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string TemplateVersion { get; set; } = string.Empty;
    public string ProviderParametersHash { get; set; } = string.Empty;
    public string SchemaVersion { get; set; } = string.Empty;
    public string CompletionHash { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public double DurationMs { get; set; }
    public int RetryCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties
    public virtual ResearchSessionEntity? Session { get; set; }
}
