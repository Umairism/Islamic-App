using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class ResearchNoteEntity
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Markdown { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    public virtual WorkspaceEntity? Workspace { get; set; }
}
