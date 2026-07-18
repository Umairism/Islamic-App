using System;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class CitationEntity
{
    public Guid Id { get; set; }
    public string CanonicalReference { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string Arabic { get; set; } = string.Empty;
    public string Translation { get; set; } = string.Empty;
    public string Book { get; set; } = string.Empty;
    public string Chapter { get; set; } = string.Empty;
    public string Verse { get; set; } = string.Empty;
    public string HadithNumber { get; set; } = string.Empty;
    public string Edition { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
}
