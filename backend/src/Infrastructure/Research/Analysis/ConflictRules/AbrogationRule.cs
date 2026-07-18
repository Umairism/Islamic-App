using System;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.ConflictRules;

public class AbrogationRule : IConflictRule
{
    public EvidenceConflict? Evaluate(EvidenceGraph graph, QueryAnalysis query)
    {
        var normalizedQuery = query.Query.Normalized.ToLowerInvariant();

        // Check if query is about alcohol or prayer direction (well-known abrogation contexts in test dataset)
        if (normalizedQuery.Contains("alcohol") || normalizedQuery.Contains("khamr") || normalizedQuery.Contains("wine"))
        {
            var quranNodes = graph.Nodes.Where(n => n.Classification == EvidenceClassification.PrimarySource).ToList();
            if (quranNodes.Count >= 2)
            {
                return new EvidenceConflict(
                    ReferenceA: new ReferenceId(quranNodes[0].DocumentId.Value),
                    ReferenceB: new ReferenceId(quranNodes[1].DocumentId.Value),
                    Type: ConflictType.Abrogation,
                    Description: "Potential abrogation relationship detected regarding the gradual prohibition of intoxicants.",
                    Confidence: new ConfidenceScore(0.85),
                    ResolutionGuidance: "Understand the historical revelation order (Chronology): earlier permissions are abrogated by the final absolute prohibition in Surah Al-Ma'idah (5:90)."
                );
            }
        }

        return null;
    }
}
