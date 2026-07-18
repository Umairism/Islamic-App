using System;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Analysis;

namespace IslamicApp.Application.Research.Events;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

public record EvidenceDeduplicatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    int OriginalCount,
    int DedupedCount
) : IDomainEvent;

public record GraphBuiltEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    int NodeCount,
    int EdgeCount
) : IDomainEvent;

public record ConflictDetectedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    int ConflictCount
) : IDomainEvent;

public record MethodologySelectedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    ResearchMethodologyType MethodologyType
) : IDomainEvent;
