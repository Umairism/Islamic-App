using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Memory;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Memory;

public class IterationPlanner : IIterationPlanner
{
    private readonly IConfidenceCalculator _confidenceCalculator;

    public IterationPlanner(IConfidenceCalculator confidenceCalculator)
    {
        _confidenceCalculator = confidenceCalculator;
    }

    public IterationDecision Plan(
        IterationContext context,
        ValidationReport validation,
        ReasoningResult reasoning,
        ReasoningBudget budget)
    {
        var allIssues = validation.ClaimValidation.Issues
            .Concat(validation.CitationValidation.Issues)
            .Concat(validation.ConsistencyValidation.Issues)
            .ToList();

        // 1. Calculate Confidence
        var currentConfidence = new CompositeConfidence(
            Evidence: validation.Passed ? 1.0 : 0.7,
            Citation: validation.CitationValidation.Passed ? 1.0 : 0.6,
            Validation: validation.Passed ? 1.0 : 0.8,
            Reasoning: reasoning.Claims.Count > 0 ? 0.95 : 0.5,
            Methodology: 1.0
        );

        var confidenceResult = _confidenceCalculator.Calculate(currentConfidence);

        // 2. Check budgets
        if (budget.RemainingIterations <= 0)
        {
            return new IterationDecision(false, IterationTerminationReason.MaxIterations, new List<RetrievalPlan>(), confidenceResult);
        }

        if (budget.RemainingExecutionTime <= TimeSpan.Zero)
        {
            return new IterationDecision(false, IterationTerminationReason.CancellationRequested, new List<RetrievalPlan>(), confidenceResult);
        }

        // Check for confidence improvement diminishing returns
        if (context.History.Count > 0)
        {
            var lastRecord = context.History.Last();
            var delta = confidenceResult.Score - lastRecord.ConfidenceResult.Score;
            if (delta > 0 && delta < 0.02)
            {
                return new IterationDecision(false, IterationTerminationReason.ConfidenceReached, new List<RetrievalPlan>(), confidenceResult);
            }
        }

        // 3. Detect gaps & map to strategy retrieval plans
        var plans = new List<RetrievalPlan>();
        var correlationId = Guid.NewGuid();

        if (!validation.Passed)
        {
            // Create target plans based on failure rules
            foreach (var issue in allIssues.Where(i => i.Severity == ErrorSeverity.Error || i.Severity == ErrorSeverity.Critical))
            {
                var gapType = issue.RuleName switch
                {
                    "CitationValidation" => KnowledgeGapType.WeakCitation,
                    "ClaimValidation" => KnowledgeGapType.MissingPrimaryEvidence,
                    _ => KnowledgeGapType.LowConfidence
                };

                string searchQuery = reasoning.Claims.FirstOrDefault()?.Statement ?? "quran hadith primary source";

                var plan = new RetrievalPlan(
                    PlanId: Guid.NewGuid(),
                    CorrelationId: correlationId,
                    Iteration: context.CurrentIteration + 1,
                    CreatedAt: DateTimeOffset.UtcNow,
                    Gap: gapType,
                    Query: searchQuery,
                    MaxResults: 5,
                    Priority: SearchPriority.High,
                    Scope: SearchScope.Workspace
                );

                plans.Add(plan);
            }
        }

        if (plans.Count == 0)
        {
            return new IterationDecision(false, IterationTerminationReason.ConfidenceReached, new List<RetrievalPlan>(), confidenceResult);
        }

        return new IterationDecision(true, null, plans, confidenceResult);
    }
}
