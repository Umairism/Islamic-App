using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.Methodologies;

public class ChronologicalMethodology : ResearchMethodologyBase
{
    public override ResearchMethodologyType Type => ResearchMethodologyType.Chronological;

    public override IReadOnlyList<ResearchEvidence> OrderEvidence(IReadOnlyList<ResearchEvidence> evidence)
    {
        // Sort chronologically by reference string (e.g. 2:255 comes before 17:23)
        return evidence.OrderBy(e => e.Reference.Value).ToList();
    }

    public override IReadOnlyList<string> GetRequiredOutputSections()
    {
        return new List<string> { "Introduction", "Chronological Revelation Sequence", "Evolution of Ruling", "Conclusion" };
    }
}
