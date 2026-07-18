using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Infrastructure.Research.Analysis.Methodologies;

public class AqidahMethodology : ResearchMethodologyBase
{
    public override ResearchMethodologyType Type => ResearchMethodologyType.Aqidah;

    public override IReadOnlyList<ResearchEvidence> OrderEvidence(IReadOnlyList<ResearchEvidence> evidence)
    {
        // Prioritize Quranic creedal content first
        return evidence
            .OrderBy(e => e.Source == EvidenceSource.Quran ? 0 : 1)
            .ThenByDescending(e => e.RetrievalScore)
            .ToList();
    }

    public override IReadOnlyList<string> GetRequiredOutputSections()
    {
        return new List<string> { "Introduction", "Core Creedal Text", "Theological Implication", "Sectarian Views", "Conclusion" };
    }
}
