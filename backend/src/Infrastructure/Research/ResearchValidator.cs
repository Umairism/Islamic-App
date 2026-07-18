using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research;

public class ResearchValidator : IResearchValidator
{
    private readonly IEnumerable<IValidationRule> _rules;

    public ResearchValidator(IEnumerable<IValidationRule> rules)
    {
        _rules = rules;
    }

    public ValidationReport ValidateAll(ReasoningResult reasoning, ResearchContext context)
    {
        var allIssues = new List<ValidationIssue>();

        foreach (var rule in _rules)
        {
            var issues = rule.Evaluate(reasoning, context);
            if (issues != null)
            {
                allIssues.AddRange(issues);
            }
        }

        var claimIssues = allIssues.Where(i => i.RuleName == "ClaimValidation").ToList();
        var citationIssues = allIssues.Where(i => i.RuleName == "CitationValidation").ToList();
        var consistencyIssues = allIssues.Where(i => i.RuleName == "ConsistencyValidation").ToList();

        var otherIssues = allIssues.Where(i => i.RuleName != "ClaimValidation" && 
                                              i.RuleName != "CitationValidation" && 
                                              i.RuleName != "ConsistencyValidation").ToList();
        if (otherIssues.Count > 0)
        {
            consistencyIssues.AddRange(otherIssues);
        }

        return new ValidationReport(
            ClaimValidation: new ClaimValidationReport(claimIssues),
            CitationValidation: new CitationValidationReport(citationIssues),
            ConsistencyValidation: new ConsistencyValidationReport(consistencyIssues)
        );
    }
}
