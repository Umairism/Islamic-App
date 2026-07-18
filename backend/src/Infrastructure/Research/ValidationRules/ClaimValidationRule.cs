using System.Collections.Generic;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.ValidationRules;

public class ClaimValidationRule : IValidationRule
{
    public string RuleName => "ClaimValidation";

    public IEnumerable<ValidationIssue> Evaluate(ReasoningResult reasoning, ResearchContext context)
    {
        var issues = new List<ValidationIssue>();

        foreach (var claim in reasoning.Claims)
        {
            if (claim.Origin == ClaimOrigin.ExternalKnowledge)
            {
                issues.Add(new ValidationIssue(
                    RuleName: RuleName,
                    Description: $"Claim relies on external model knowledge not present in the retrieve evidence corpus: '{claim.Statement}'",
                    Severity: ErrorSeverity.Warning,
                    AffectedReferences: claim.SupportingEvidence
                ));
            }

            if (claim.Confidence.Value < 0.4)
            {
                issues.Add(new ValidationIssue(
                    RuleName: RuleName,
                    Description: $"Claim has dangerously low confidence score ({claim.Confidence.Value:F2}): '{claim.Statement}'",
                    Severity: ErrorSeverity.Error,
                    AffectedReferences: claim.SupportingEvidence
                ));
            }
        }

        return issues;
    }
}
