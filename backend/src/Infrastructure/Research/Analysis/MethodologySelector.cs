using System;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis;

public class MethodologySelector : IMethodologySelector
{
    public ResearchMethodologyType ResolveMethodology(QueryAnalysis analysis)
    {
        var query = analysis.Query.Normalized.ToLowerInvariant();

        if (query.Contains("chronological") || query.Contains("revelation order") || query.Contains("timeline"))
        {
            return ResearchMethodologyType.Chronological;
        }

        if (query.Contains("fiqh") || query.Contains("ruling") || query.Contains("legal") || query.Contains("halal") || query.Contains("haram"))
        {
            return ResearchMethodologyType.Fiqh;
        }

        if (query.Contains("linguistic") || query.Contains("meaning") || query.Contains("root") || query.Contains("grammar"))
        {
            return ResearchMethodologyType.Linguistic;
        }

        if (query.Contains("tafsir") || query.Contains("commentary") || query.Contains("kathir"))
        {
            return ResearchMethodologyType.Tafsir;
        }

        if (query.Contains("aqidah") || query.Contains("creed") || query.Contains("belief") || query.Contains("tawhid"))
        {
            return ResearchMethodologyType.Aqidah;
        }

        if (query.Contains("compare") || query.Contains("difference"))
        {
            return ResearchMethodologyType.Comparative;
        }

        if (query.Contains("history") || query.Contains("context") || query.Contains("asbab"))
        {
            return ResearchMethodologyType.Historical;
        }

        if (query.Contains("literal") || query.Contains("direct"))
        {
            return ResearchMethodologyType.Literal;
        }

        return ResearchMethodologyType.Thematic; // Default thematic methodology
    }
}
