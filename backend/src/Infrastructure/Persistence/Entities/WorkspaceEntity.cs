using System;
using System.Collections.Generic;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class WorkspaceEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<ResearchSessionEntity> Sessions { get; set; } = new List<ResearchSessionEntity>();
    public virtual ICollection<ResearchNoteEntity> Notes { get; set; } = new List<ResearchNoteEntity>();
    public virtual ICollection<BookmarkEntity> Bookmarks { get; set; } = new List<BookmarkEntity>();
}
