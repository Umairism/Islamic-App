using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.Methodologies;

public class ThematicMethodology : ResearchMethodologyBase
{
    public override ResearchMethodologyType Type => ResearchMethodologyType.Thematic;

    public override IReadOnlyList<ResearchEvidence> OrderEvidence(IReadOnlyList<ResearchEvidence> evidence)
    {
        // Group by primary topic (first topic) and sort by score descending
        return evidence
            .OrderBy(e => e.Topics.FirstOrDefault().Value ?? string.Empty)
            .ThenByDescending(e => e.RetrievalScore)
            .ToList();
    }

    public override IReadOnlyList<string> GetRequiredOutputSections()
    {
        return new List<string> { "Introduction", "Thematic Categorization", "Synthesized Analysis", "Modern Implications" };
    }
}
