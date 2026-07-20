using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class ResearchDossierEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ResearchSessionId { get; set; }

    public string Format { get; set; } = "Markdown";

    public string ContentHash { get; set; } = string.Empty;

    public string StoragePath { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
