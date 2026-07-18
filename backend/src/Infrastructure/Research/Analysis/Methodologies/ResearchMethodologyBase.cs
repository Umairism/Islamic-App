using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.Methodologies;

public abstract class ResearchMethodologyBase : IResearchMethodology
{
    public abstract ResearchMethodologyType Type { get; }

    public virtual IReadOnlyList<ResearchEvidence> OrderEvidence(IReadOnlyList<ResearchEvidence> evidence)
    {
        return evidence.OrderByDescending(e => e.RetrievalScore).ToList();
    }

    public virtual IReadOnlyList<string> GetRequiredOutputSections()
    {
        return new List<string> { "Introduction", "Evidence Analysis", "Conclusion" };
    }
}
