using System;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Analysis;

namespace IslamicApp.Application.Research.Events;

public interface IDomainEvent : MediatR.INotification
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

public record ResearchStartedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SessionId,
    string Query,
    Guid WorkspaceId
) : IDomainEvent;

public record ResearchExecutedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SessionId,
    Guid WorkspaceId,
    ResearchPrompt Prompt,
    GenerationResponse Response,
    GenerationMetadata Metadata
) : IDomainEvent;

public record ResearchValidatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SessionId,
    Guid WorkspaceId,
    ReasoningResult Reasoning,
    ValidationReport Report,
    ExplainabilityMap Explainability
) : IDomainEvent;

public record ResearchPublishedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SessionId,
    Guid WorkspaceId,
    Guid DocumentId,
    Guid RevisionId,
    string Summary
) : IDomainEvent;

public record DocumentCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid DocumentId,
    Guid SessionId,
    Guid WorkspaceId,
    string Title
) : IDomainEvent;

public record RevisionCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid RevisionId,
    Guid DocumentId,
    int RevisionNumber,
    string Summary
) : IDomainEvent;

public record WorkspaceCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid WorkspaceId,
    string Name
) : IDomainEvent;

public record WorkspaceExportedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid WorkspaceId,
    string ExportFormat,
    Guid JobId
) : IDomainEvent;

public record BookmarkAddedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid BookmarkId,
    Guid WorkspaceId,
    Guid SessionId,
    string ReferenceId
) : IDomainEvent;

public record NoteCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid NoteId,
    Guid WorkspaceId,
    string Title
) : IDomainEvent;

public record ValidationFailedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SessionId,
    Guid WorkspaceId,
    ValidationReport Report
) : IDomainEvent;

public record ResearchSessionQueuedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SessionId,
    Guid WorkspaceId
) : IDomainEvent;

public record ResearchSessionStartedEvent(
    Guid SessionId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : MediatR.INotification;

public record ResearchSessionCompletedEvent(
    Guid SessionId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : MediatR.INotification;

public record ResearchSessionCancelledEvent(
    Guid SessionId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : MediatR.INotification;

public record ResearchSessionFailedEvent(
    Guid SessionId,
    Guid WorkspaceId,
    string ErrorMessage,
    DateTimeOffset OccurredAt
) : MediatR.INotification;

public record ResearchStageCompletedEvent(
    Guid SessionId,
    string Stage,
    int Progress,
    string Message,
    DateTimeOffset OccurredAt
) : MediatR.INotification;
