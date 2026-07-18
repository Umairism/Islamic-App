using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

namespace IslamicApp.Infrastructure.Research.Analysis;

public class ResearchPipeline : IResearchPipeline
{
    private readonly List<IResearchPipelineBehavior> _behaviors;

    public ResearchPipeline(IEnumerable<IResearchPipelineBehavior> behaviors)
    {
        // Enforce frozen execution order: Exception -> Metrics -> Logging -> Retrieval -> Deduplication -> Analysis
        var behaviorsList = behaviors.ToList();
        _behaviors = new List<IResearchPipelineBehavior>();
        
        AddBehaviorIfRegistered<ExceptionBehavior>(behaviorsList);
        AddBehaviorIfRegistered<MetricsBehavior>(behaviorsList);
        AddBehaviorIfRegistered<LoggingBehavior>(behaviorsList);
        AddBehaviorIfRegistered<RetrievalBehavior>(behaviorsList);
        AddBehaviorIfRegistered<DeduplicationBehavior>(behaviorsList);
        AddBehaviorIfRegistered<AnalysisBehavior>(behaviorsList);

        // Add any remaining custom behaviors that might be registered
        foreach (var b in behaviorsList)
        {
            if (!_behaviors.Contains(b))
            {
                _behaviors.Add(b);
            }
        }
    }

    private void AddBehaviorIfRegistered<T>(List<IResearchPipelineBehavior> registered) where T : IResearchPipelineBehavior
    {
        var behavior = registered.OfType<T>().FirstOrDefault();
        if (behavior != null)
        {
            _behaviors.Add(behavior);
        }
    }

    public async Task<Result<ResearchExecutionContext>> ExecuteAsync(QueryAnalysis query, CancellationToken cancellationToken)
    {
        var context = new ResearchContext(new ResearchInput(query, null));
        var executionContext = new ResearchExecutionContext(context, ImmutableList<IDomainEvent>.Empty, PipelineStage.Retrieval);

        int behaviorIndex = 0;

        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>>? next = null;
        next = async (currentContext) =>
        {
            if (behaviorIndex < _behaviors.Count)
            {
                var behavior = _behaviors[behaviorIndex++];
                return await behavior.HandleAsync(currentContext, next!, cancellationToken);
            }

            return Result<ResearchExecutionContext>.Success(currentContext);
        };

        return await next(executionContext);
    }
}
