using System.Collections.Generic;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Research.Models;

public class EvidenceMatch
{
    public EvidenceSource Source { get; init; }
    public string Collection { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public string PrimaryText { get; init; } = string.Empty;
    public IReadOnlyList<TranslationDto> Translations { get; init; } = new List<TranslationDto>();
    public EvidenceMetadata Metadata { get; init; } = null!;
    public double Score { get; set; }
    public List<string> Reasons { get; } = new();
    public List<string> MatchedTerms { get; } = new();
}
