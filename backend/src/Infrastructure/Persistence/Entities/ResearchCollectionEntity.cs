using System;
using System.Collections.Generic;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class ResearchCollectionEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<ResearchSessionCollectionEntity> SessionCollections { get; set; } = new List<ResearchSessionCollectionEntity>();
}
