using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis;

public class ConflictDetector : IConflictDetector
{
    private readonly IEnumerable<IConflictRule> _rules;

    public ConflictDetector(IEnumerable<IConflictRule> rules)
    {
        _rules = rules;
    }

    public ConflictAnalysis DetectConflicts(EvidenceGraph graph, QueryAnalysis query)
    {
        var conflicts = new List<EvidenceConflict>();

        foreach (var rule in _rules)
        {
            var conflict = rule.Evaluate(graph, query);
            if (conflict != null)
            {
                conflicts.Add(conflict);
            }
        }

        var summary = conflicts.Count > 0
            ? $"Found {conflicts.Count} potential textual conflict(s) or interpretation differences."
            : "No significant textual conflicts detected.";

        return new ConflictAnalysis(conflicts, conflicts.Count > 0, summary);
    }
}
