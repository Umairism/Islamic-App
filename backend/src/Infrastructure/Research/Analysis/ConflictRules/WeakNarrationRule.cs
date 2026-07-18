using System;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.ConflictRules;

public class WeakNarrationRule : IConflictRule
{
    public EvidenceConflict? Evaluate(EvidenceGraph graph, QueryAnalysis query)
    {
        // Rule: If a node is classified as SecondarySource (Hadith) and has Low confidence score, flag it
        foreach (var node in graph.Nodes)
        {
            if (node.Classification == EvidenceClassification.SecondarySource && 
                node.ConfidenceScore.Level == ConfidenceLevel.Low)
            {
                return new EvidenceConflict(
                    ReferenceA: new ReferenceId("General Hadith"),
                    ReferenceB: new ReferenceId(node.DocumentId.Value),
                    Type: ConflictType.WeakNarration,
                    Description: $"Narration {node.DocumentId.Value} has low retrieval score or grading confidence.",
                    Confidence: node.ConfidenceScore,
                    ResolutionGuidance: "Verify the chain of transmission (Isnad) or reference classical Hadith commentaries."
                );
            }
        }

        return null;
    }
}
