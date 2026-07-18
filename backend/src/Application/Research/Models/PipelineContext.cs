using System;
using System.Collections.Immutable;
using IslamicApp.Application.Research.Events;

namespace IslamicApp.Application.Research.Models;

public enum PipelineStage
{
    Retrieval,
    Deduplication,
    Analysis,
    Completed,
    Failed
}

public record ResearchExecutionContext(
    ResearchContext Context,
    ImmutableList<IDomainEvent> Events,
    PipelineStage CurrentStage
)
{
    public ResearchExecutionContext WithContext(ResearchContext context) =>
        this with { Context = context };

    public ResearchExecutionContext Raise(IDomainEvent evt) =>
        this with { Events = Events.Add(evt) };

    public ResearchExecutionContext TransitionTo(PipelineStage stage) =>
        this with { CurrentStage = stage };
}
