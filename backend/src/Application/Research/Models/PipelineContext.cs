using System;
using System.Collections.Immutable;
using IslamicApp.Application.Research.Events;

using IslamicApp.Application.Research.Memory;

namespace IslamicApp.Application.Research.Models;

public enum PipelineStage
{
    Retrieval,
    Deduplication,
    Analysis,
    Reasoning,
    Validation,
    Explainability,
    Evaluation,
    Rendering,
    DossierGeneration,
    Completed,
    Failed
}

public record PipelineStageExecution(
    PipelineStage Stage,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    TimeSpan Duration
);

public record ResearchExecutionContext(
    ResearchContext Context,
    System.Collections.Immutable.ImmutableList<IDomainEvent> Events,
    PipelineStage CurrentStage,
    System.Collections.Immutable.ImmutableList<PipelineStageExecution> StageExecutions,
    ReasoningSession Session = null,
    ReasoningResult Reasoning = null,
    ValidationReport Validation = null,
    ExplainabilityMap Explainability = null,
    System.Collections.Immutable.ImmutableList<RenderResult> RenderedOutputs = null,
    MemorySelectionResult Memory = null,
    IterationContext Iteration = null,
    ResearchExecutionContextMetadata Metadata = null,
    ResearchExecutionMetrics Metrics = null
)
{
    public ResearchExecutionContext WithContext(ResearchContext context) =>
        this with { Context = context };

    public ResearchExecutionContext Raise(IDomainEvent evt) =>
        this with { Events = Events.Add(evt) };

    public ResearchExecutionContext TransitionTo(PipelineStage stage) =>
        this with { CurrentStage = stage };

    public ResearchExecutionContext WithStageExecution(PipelineStageExecution execution) =>
        this with { StageExecutions = StageExecutions.Add(execution) };

    public ResearchExecutionContext WithReasoning(ReasoningSession session, ReasoningResult reasoning) =>
        this with { Session = session, Reasoning = reasoning };

    public ResearchExecutionContext WithValidation(ValidationReport validation) =>
        this with { Validation = validation };

    public ResearchExecutionContext WithExplainability(ExplainabilityMap explainability) =>
        this with { Explainability = explainability };

    public ResearchExecutionContext WithRenderedOutputs(System.Collections.Immutable.ImmutableList<RenderResult> outputs) =>
        this with { RenderedOutputs = outputs };

    public ResearchExecutionContext WithMemory(MemorySelectionResult memory) =>
        this with { Memory = memory };

    public ResearchExecutionContext WithIteration(IterationContext iteration) =>
        this with { Iteration = iteration };

    public ResearchExecutionContext WithMetadata(ResearchExecutionContextMetadata metadata) =>
        this with { Metadata = metadata };

    public ResearchExecutionContext WithMetrics(ResearchExecutionMetrics metrics) =>
        this with { Metrics = metrics };
}
