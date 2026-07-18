using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research;

public class OutputGuard : IOutputGuard
{
    private readonly IEnumerable<IResearchRenderer> _renderers;

    public OutputGuard(IEnumerable<IResearchRenderer> renderers)
    {
        _renderers = renderers;
    }

    public Result<ResearchResult> EvaluatePublishability(
        ResearchExecutionContext executionContext,
        ReasoningSession session,
        ReasoningResult reasoning,
        ValidationReport validation,
        ExplainabilityMap explainability)
    {
        if (!validation.Passed)
        {
            var criticalIssues = GetCriticalIssues(validation);
            var issueDescription = string.Join("; ", criticalIssues.Select(i => $"[{i.RuleName}] {i.Description}"));
            
            return Result<ResearchResult>.Failure(new Error(
                Code: "PublishabilityBlocked",
                Message: $"The reasoning results failed publishability standards due to critical validation issues: {issueDescription}",
                Severity: ErrorSeverity.Error
            ));
        }

        // Initialize outputs package. We render later asynchronously in rendering behavior/stage!
        // But for domain safety, OutputGuard constructs the envelope.
        var result = new ResearchResult(
            ExecutionContext: executionContext,
            Session: session,
            Reasoning: reasoning,
            Validation: validation,
            Explainability: explainability,
            Outputs: new List<RenderResult>()
        );

        return Result<ResearchResult>.Success(result);
    }

    private List<ValidationIssue> GetCriticalIssues(ValidationReport report)
    {
        var issues = new List<ValidationIssue>();
        issues.AddRange(report.ClaimValidation.Issues.Where(i => i.Severity == ErrorSeverity.Error || i.Severity == ErrorSeverity.Critical));
        issues.AddRange(report.CitationValidation.Issues.Where(i => i.Severity == ErrorSeverity.Error || i.Severity == ErrorSeverity.Critical));
        issues.AddRange(report.ConsistencyValidation.Issues.Where(i => i.Severity == ErrorSeverity.Error || i.Severity == ErrorSeverity.Critical));
        return issues;
    }
}
