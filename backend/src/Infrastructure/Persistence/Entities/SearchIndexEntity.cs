using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class SearchIndexEntity
{
    public string EntityId { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}
