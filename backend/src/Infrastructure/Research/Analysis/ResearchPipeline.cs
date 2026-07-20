using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Memory;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;
using MediatR;

namespace IslamicApp.Infrastructure.Research.Analysis;

public class ResearchPipeline : IResearchPipeline
{
    private readonly List<IResearchPipelineBehavior> _behaviors;
    private readonly IMediator _mediator;

    public ResearchPipeline(IEnumerable<IResearchPipelineBehavior> behaviors, IMediator mediator)
    {
        _mediator = mediator;
        // Enforce frozen execution order: Exception -> Metrics -> Logging -> Retrieval -> Deduplication -> Analysis -> Reasoning -> Validation -> Explainability -> Rendering
        var behaviorsList = behaviors.ToList();
        _behaviors = new List<IResearchPipelineBehavior>();
        
        AddBehaviorIfRegistered<ExceptionBehavior>(behaviorsList);
        AddBehaviorIfRegistered<MetricsBehavior>(behaviorsList);
        AddBehaviorIfRegistered<LoggingBehavior>(behaviorsList);
        AddBehaviorIfRegistered<WorkspaceMemoryBehavior>(behaviorsList);
        AddBehaviorIfRegistered<RetrievalBehavior>(behaviorsList);
        AddBehaviorIfRegistered<DeduplicationBehavior>(behaviorsList);
        AddBehaviorIfRegistered<AnalysisBehavior>(behaviorsList);
        AddBehaviorIfRegistered<ReasoningBehavior>(behaviorsList);
        AddBehaviorIfRegistered<ValidationBehavior>(behaviorsList);
        AddBehaviorIfRegistered<IterationBehavior>(behaviorsList);
        AddBehaviorIfRegistered<ExplainabilityBehavior>(behaviorsList);
        AddBehaviorIfRegistered<EvaluationBehavior>(behaviorsList);
        AddBehaviorIfRegistered<RenderingBehavior>(behaviorsList);
        AddBehaviorIfRegistered<DossierGenerationBehavior>(behaviorsList);
        AddBehaviorIfRegistered<PersistenceBehavior>(behaviorsList);

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

    private int GetProgressForStage(string stageName) => stageName switch
    {
        "Exception" => 5,
        "Metrics" => 10,
        "Logging" => 15,
        "WorkspaceMemory" => 20,
        "Retrieval" => 30,
        "Deduplication" => 45,
        "Analysis" => 55,
        "Reasoning" => 70,
        "Validation" => 80,
        "Iteration" => 85,
        "Explainability" => 90,
        "Rendering" => 95,
        "Persistence" => 98,
        _ => 50
    };

    public async Task<Result<ResearchExecutionContext>> ExecuteAsync(
        QueryAnalysis query, 
        Guid? sessionId = null,
        Guid? workspaceId = null,
        CancellationToken cancellationToken = default)
    {
        var context = new ResearchContext(new ResearchInput(query, null));
        var executionContext = new ResearchExecutionContext(
            Context: context, 
            Events: ImmutableList<IDomainEvent>.Empty, 
            CurrentStage: PipelineStage.Retrieval,
            StageExecutions: ImmutableList<PipelineStageExecution>.Empty
        );

        var correlationId = Guid.NewGuid();
        executionContext = executionContext.WithMetadata(new ResearchExecutionContextMetadata(
            CorrelationId: correlationId,
            SessionId: sessionId ?? Guid.NewGuid(),
            WorkspaceId: workspaceId ?? Guid.Empty,
            RequestId: Guid.NewGuid()
        ));

        bool iterate = true;
        while (iterate)
        {
            int behaviorIndex = 0;
            Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>>? next = null;
            next = async (currentContext) =>
            {
                if (behaviorIndex < _behaviors.Count)
                {
                    var behavior = _behaviors[behaviorIndex++];
                    string stageName = behavior.GetType().Name.Replace("Behavior", "");
                    int progress = GetProgressForStage(stageName);

                    // Publish stage starting
                    await _mediator.Publish(new ResearchStageCompletedEvent(
                        currentContext.Metadata?.SessionId ?? Guid.Empty, 
                        stageName, 
                        progress, 
                        $"Executing stage: {stageName}", 
                        DateTimeOffset.UtcNow
                    ), cancellationToken);

                    var res = await behavior.HandleAsync(currentContext, next!, cancellationToken);

                    if (res.IsSuccess)
                    {
                        // Publish stage completed
                        await _mediator.Publish(new ResearchStageCompletedEvent(
                            currentContext.Metadata?.SessionId ?? Guid.Empty, 
                            stageName, 
                            progress + 2, 
                            $"{stageName} stage completed.", 
                            DateTimeOffset.UtcNow
                        ), cancellationToken);
                    }

                    return res;
                }

                return Result<ResearchExecutionContext>.Success(currentContext);
            };

            var loopResult = await next(executionContext);
            if (!loopResult.IsSuccess) return loopResult;

            executionContext = loopResult.Value!;

            if (executionContext.Iteration != null && executionContext.Iteration.State == PipelineState.GapDetected)
            {
                executionContext = executionContext
                    .WithIteration(executionContext.Iteration with { State = PipelineState.AdditionalRetrieval })
                    .TransitionTo(PipelineStage.Retrieval);
            }
            else
            {
                iterate = false;
            }
        }

        return Result<ResearchExecutionContext>.Success(executionContext);
    }
}
