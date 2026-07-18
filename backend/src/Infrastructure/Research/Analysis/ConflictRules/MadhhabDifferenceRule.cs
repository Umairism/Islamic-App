using System;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.ConflictRules;

public class MadhhabDifferenceRule : IConflictRule
{
    public EvidenceConflict? Evaluate(EvidenceGraph graph, QueryAnalysis query)
    {
        var normalizedQuery = query.Query.Normalized.ToLowerInvariant();

        // Check if query is about legal actions (praying posture, wudu, eating seafood) which trigger school of thoughts differences
        if (normalizedQuery.Contains("pray") || normalizedQuery.Contains("wudu") || normalizedQuery.Contains("fiqh") || normalizedQuery.Contains("seafood") || normalizedQuery.Contains("shafii") || normalizedQuery.Contains("hanafi"))
        {
            return new EvidenceConflict(
                ReferenceA: new ReferenceId("Hanafi School"),
                ReferenceB: new ReferenceId("Shafi'i School"),
                Type: ConflictType.DifferentMadhhab,
                Description: "Legal ruling interpretations differ across the classical Sunni schools of thought (Madhhahib).",
                Confidence: new ConfidenceScore(0.70),
                ResolutionGuidance: "Consult comparative Fiqh literature (e.g. Bidayat al-Mujtahid) to identify the specific textual evidences used by each school."
            );
        }

        return null;
    }
}
