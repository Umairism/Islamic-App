using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Infrastructure.Research.Analysis.Methodologies;

public class FiqhMethodology : ResearchMethodologyBase
{
    public override ResearchMethodologyType Type => ResearchMethodologyType.Fiqh;

    public override IReadOnlyList<ResearchEvidence> OrderEvidence(IReadOnlyList<ResearchEvidence> evidence)
    {
        // Prioritize Quranic legal verses, then Hadith
        return evidence
            .OrderBy(e => e.Source == EvidenceSource.Quran ? 0 : 1)
            .ThenByDescending(e => e.RetrievalScore)
            .ToList();
    }

    public override IReadOnlyList<string> GetRequiredOutputSections()
    {
        return new List<string> { "Introduction", "Legal Proofs (Adillah)", "Derived Rulings (Ahkam)", "Maxims (Qawa'id)", "Conclusion" };
    }
}
