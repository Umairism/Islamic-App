using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class ResearchSessionCollectionEntity
{
    public Guid SessionId { get; set; }
    public Guid CollectionId { get; set; }

    // Navigation properties
    public virtual ResearchSessionEntity? Session { get; set; }
    public virtual ResearchCollectionEntity? Collection { get; set; }
}
