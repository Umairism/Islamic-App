using System.Collections.Generic;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public enum ResearchMethodologyType
{
    Literal,
    Comparative,
    Fiqh,
    Aqidah,
    Linguistic,
    Historical,
    Thematic,
    Chronological,
    Tafsir
}

public interface IResearchMethodology
{
    ResearchMethodologyType Type { get; }
    IReadOnlyList<ResearchEvidence> OrderEvidence(IReadOnlyList<ResearchEvidence> evidence);
    IReadOnlyList<string> GetRequiredOutputSections();
}
