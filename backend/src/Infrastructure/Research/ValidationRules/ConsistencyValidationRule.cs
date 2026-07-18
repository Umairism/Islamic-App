using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.ValidationRules;

public class ConsistencyValidationRule : IValidationRule
{
    public string RuleName => "ConsistencyValidation";

    public IEnumerable<ValidationIssue> Evaluate(ReasoningResult reasoning, ResearchContext context)
    {
        var issues = new List<ValidationIssue>();

        // Heuristic: If there are contradicting claims in the same reasoning output (e.g. one claims alcohol is completely prohibited and another says it is permitted)
        var claims = reasoning.Claims.ToList();
        for (int i = 0; i < claims.Count; i++)
        {
            for (int j = i + 1; j < claims.Count; j++)
            {
                var stmtA = claims[i].Statement.ToLowerInvariant();
                var stmtB = claims[j].Statement.ToLowerInvariant();

                // Simple check for conflicting binary stances (e.g., prohibited vs permitted)
                if (stmtA.Contains("prohibited") && stmtB.Contains("permitted") && HaveSharedReferences(claims[i], claims[j]))
                {
                    issues.Add(new ValidationIssue(
                        RuleName: RuleName,
                        Description: $"Potential logical contradiction detected between claim '{claims[i].Statement}' and claim '{claims[j].Statement}'.",
                        Severity: ErrorSeverity.Warning,
                        AffectedReferences: claims[i].SupportingEvidence.Concat(claims[j].SupportingEvidence).Distinct().ToList()
                    ));
                }
            }
        }

        return issues;
    }

    private bool HaveSharedReferences(ResearchClaim a, ResearchClaim b)
    {
        var set = new HashSet<string>(a.SupportingEvidence.Select(r => r.Value.ToLowerInvariant()));
        return b.SupportingEvidence.Any(r => set.Contains(r.Value.ToLowerInvariant()));
    }
}
