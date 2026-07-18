using System.Collections.Generic;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.Methodologies;

public class LiteralMethodology : ResearchMethodologyBase
{
    public override ResearchMethodologyType Type => ResearchMethodologyType.Literal;

    public override IReadOnlyList<string> GetRequiredOutputSections()
    {
        return new List<string> { "Introduction", "Textual Manifestations", "Explicit Directives", "Conclusion" };
    }
}
