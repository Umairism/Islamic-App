using System;

namespace IslamicApp.Application.Research.Models;

public record Citation(
    Guid Id,
    string CanonicalReference,
    string SourceType,
    string Arabic,
    string Translation,
    string Book,
    string Chapter,
    string Verse,
    string HadithNumber,
    string Edition,
    string Url,
    string Checksum
);
