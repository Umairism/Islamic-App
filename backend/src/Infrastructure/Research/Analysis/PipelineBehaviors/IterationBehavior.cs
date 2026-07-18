using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Memory;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class IterationBehavior : IResearchPipelineBehavior
{
    private readonly IIterationPlanner _planner;
    private readonly IResearchFeatureFlags _featureFlags;

    public IterationBehavior(IIterationPlanner planner, IResearchFeatureFlags featureFlags)
    {
        _planner = planner;
        _featureFlags = featureFlags;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        if (!_featureFlags.EnableReasoning)
        {
            return await next(executionContext);
        }

        // Initialize IterationContext if null
        var currentIteration = executionContext.Iteration ?? new IterationContext(
            CurrentIteration: 0,
            State: PipelineState.Initial,
            Confidence: new CompositeConfidence(1.0, 1.0, 1.0, 1.0, 1.0),
            History: new List<IterationRecord>(),
            PendingGaps: new List<EvidenceGap>(),
            RetrievedEvidence: new List<string>()
        );

        // Determine budget
        var budget = new ReasoningBudget(
            RemainingIterations: 3 - currentIteration.CurrentIteration,
            RemainingTokens: 50000,
            RemainingExecutionTime: TimeSpan.FromMinutes(5)
        );

        // Plan iteration
        var decision = _planner.Plan(currentIteration, executionContext.Validation, executionContext.Reasoning, budget);

        // Map RetrievalPlans to EvidenceGaps
        var gaps = decision.Plans.Select(p => new EvidenceGap(
            GapType: p.Gap,
            Priority: (int)p.Priority,
            RetrievalStrategy: p.Scope.ToString(),
            SearchTerms: p.Query,
            MaxResults: p.MaxResults
        )).ToList();

        var evidences = executionContext.Context.Input.Corpus?.Evidences ?? new List<ResearchEvidence>();
        var record = new IterationRecord(
            Iteration: currentIteration.CurrentIteration,
            RetrievedNodes: evidences.Select(e => e.Id.Value).ToList(),
            NewEvidence: evidences.Select(e => e.Reference.Value).ToList(),
            ConfidenceResult: decision.Confidence,
            KnowledgeGaps: gaps,
            Duration: TimeSpan.FromSeconds(1)
        );

        var newHistory = new List<IterationRecord>(currentIteration.History) { record };

        IterationContext updatedIteration;
        if (decision.Continue)
        {
            updatedIteration = currentIteration with
            {
                CurrentIteration = currentIteration.CurrentIteration + 1,
                State = PipelineState.GapDetected,
                Confidence = new CompositeConfidence(
                    decision.Confidence.Components["Evidence"],
                    decision.Confidence.Components["Citation"],
                    decision.Confidence.Components["Validation"],
                    decision.Confidence.Components["Reasoning"],
                    decision.Confidence.Components["Methodology"]
                ),
                History = newHistory,
                PendingGaps = gaps
            };
        }
        else
        {
            updatedIteration = currentIteration with
            {
                State = PipelineState.Completed,
                Confidence = new CompositeConfidence(
                    decision.Confidence.Components["Evidence"],
                    decision.Confidence.Components["Citation"],
                    decision.Confidence.Components["Validation"],
                    decision.Confidence.Components["Reasoning"],
                    decision.Confidence.Components["Methodology"]
                ),
                History = newHistory,
                PendingGaps = new List<EvidenceGap>()
            };
        }

        var enrichedContext = executionContext.WithIteration(updatedIteration);

        return await next(enrichedContext);
    }
}
