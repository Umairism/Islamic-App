using System;
using System.Collections.Generic;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class ResearchDocumentEntity
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid WorkspaceId { get; set; }
    public Guid? CurrentRevisionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties
    public virtual ResearchSessionEntity? Session { get; set; }
    public virtual ICollection<DocumentRevisionEntity> Revisions { get; set; } = new List<DocumentRevisionEntity>();
}
