using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.ValidationRules;

public class CitationValidationRule : IValidationRule
{
    public string RuleName => "CitationValidation";

    public IEnumerable<ValidationIssue> Evaluate(ReasoningResult reasoning, ResearchContext context)
    {
        var issues = new List<ValidationIssue>();

        var allowedRefs = new HashSet<string>(
            context.Input.Corpus?.Evidences.Select(e => e.Reference.Value.ToLowerInvariant()) 
            ?? Enumerable.Empty<string>()
        );

        // Check claims supporting evidence
        foreach (var claim in reasoning.Claims)
        {
            foreach (var evidenceRef in claim.SupportingEvidence)
            {
                if (!allowedRefs.Contains(evidenceRef.Value.ToLowerInvariant()))
                {
                    issues.Add(new ValidationIssue(
                        RuleName: RuleName,
                        Description: $"Claim references a fabricated citation '{evidenceRef.Value}' which is not present in the allowed evidence corpus.",
                        Severity: ErrorSeverity.Error,
                        AffectedReferences: new List<ReferenceId> { evidenceRef }
                    ));
                }
            }
        }

        // Check findings cited references
        foreach (var finding in reasoning.Findings)
        {
            foreach (var refId in finding.CitedReferences)
            {
                if (!allowedRefs.Contains(refId.Value.ToLowerInvariant()))
                {
                    issues.Add(new ValidationIssue(
                        RuleName: RuleName,
                        Description: $"Finding '{finding.Heading}' cites a reference '{refId.Value}' not present in the retrieved evidence corpus.",
                        Severity: ErrorSeverity.Error,
                        AffectedReferences: new List<ReferenceId> { refId }
                    ));
                }
            }
        }

        return issues;
    }
}
