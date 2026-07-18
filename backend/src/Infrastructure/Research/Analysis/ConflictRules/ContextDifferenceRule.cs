using System;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.ConflictRules;

public class ContextDifferenceRule : IConflictRule
{
    public EvidenceConflict? Evaluate(EvidenceGraph graph, QueryAnalysis query)
    {
        var normalizedQuery = query.Query.Normalized.ToLowerInvariant();

        // Check if query implies context specificity (e.g. fighting, peace, parent relation limits)
        if (normalizedQuery.Contains("fight") || normalizedQuery.Contains("war") || normalizedQuery.Contains("peace"))
        {
            return new EvidenceConflict(
                ReferenceA: new ReferenceId("General Peace Verses"),
                ReferenceB: new ReferenceId("Specific Defense Verses"),
                Type: ConflictType.ContextDifference,
                Description: "Apparent tension between directives of peace and defensive combat.",
                Confidence: new ConfidenceScore(0.80),
                ResolutionGuidance: "Resolve by understanding Context (Asbab al-Nuzul). Peace verses apply in general relations, whereas defensive combat directives apply to state-level warfare contexts."
            );
        }

        return null;
    }
}
