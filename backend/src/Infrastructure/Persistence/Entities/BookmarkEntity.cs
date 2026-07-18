using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class BookmarkEntity
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid ResearchSessionId { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties
    public virtual WorkspaceEntity? Workspace { get; set; }
    public virtual ResearchSessionEntity? Session { get; set; }
}
