using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IslamicApp.Application.Research.Events;
using IslamicApp.Application.Research.Models;
using IslamicApp.Infrastructure.Persistence.Entities;

namespace IslamicApp.Infrastructure.Persistence.EventHandlers;

public class SaveSessionSnapshotHandler : 
    INotificationHandler<ResearchStartedEvent>,
    INotificationHandler<ResearchExecutedEvent>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SaveSessionSnapshotHandler> _logger;

    public SaveSessionSnapshotHandler(ApplicationDbContext dbContext, ILogger<SaveSessionSnapshotHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(ResearchStartedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ResearchStartedEvent for Session {SessionId}", notification.SessionId);

        // Ensure default workspace exists
        var workspace = await _dbContext.Workspaces.FindAsync(new object[] { notification.WorkspaceId }, cancellationToken);
        if (workspace == null)
        {
            workspace = new WorkspaceEntity
            {
                Id = notification.WorkspaceId,
                Name = "Default Workspace",
                Description = "Auto-created workspace for persistence",
                CreatedAt = DateTimeOffset.UtcNow
            };
            _dbContext.Workspaces.Add(workspace);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Add ResearchSessionEntity
        var session = new ResearchSessionEntity
        {
            Id = notification.SessionId,
            WorkspaceId = notification.WorkspaceId,
            Title = $"Research: {notification.Query[..Math.Min(notification.Query.Length, 30)]}...",
            Query = notification.Query,
            CreatedAt = notification.OccurredAt,
            Methodology = "Thematic",
            Language = "English",
            ConfidenceValue = 1.0,
            ConfidenceLevel = "VeryHigh",
            Status = "Started"
        };

        _dbContext.ResearchSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(ResearchExecutedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ResearchExecutedEvent for Session {SessionId}", notification.SessionId);

        var session = await _dbContext.ResearchSessions.FindAsync(new object[] { notification.SessionId }, cancellationToken);
        if (session != null)
        {
            session.Status = "Executed";
            session.Methodology = notification.Prompt.Variables.Methodology.ToString();
        }

        // Create ExecutionSnapshot
        var promptText = notification.Prompt.RenderedSystemPrompt + " " + notification.Prompt.RenderedUserPrompt;
        var responseText = notification.Response.Content;

        var snapshot = new ResearchExecutionSnapshotEntity
        {
            Id = Guid.NewGuid(),
            ResearchSessionId = notification.SessionId,
            Provider = notification.Metadata.Provider,
            Model = notification.Metadata.Model,
            PromptHash = ComputeHash(promptText),
            PromptVersion = notification.Prompt.Template.Version,
            TemplateVersion = notification.Prompt.Template.Version,
            ProviderParametersHash = ComputeHash(JsonSerializer.Serialize(notification.Prompt.Template.ConfigMetadata)),
            SchemaVersion = "1.0.0",
            CompletionHash = ComputeHash(responseText),
            PromptTokens = notification.Metadata.PromptTokens,
            CompletionTokens = notification.Metadata.CompletionTokens,
            DurationMs = notification.Metadata.Duration.TotalMilliseconds,
            RetryCount = notification.Metadata.FinishReason == FinishReason.Stop ? 0 : 1,
            CreatedAt = notification.OccurredAt
        };

        _dbContext.ExecutionSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private string ComputeHash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}

public class SaveDocumentHandler : INotificationHandler<ResearchPublishedEvent>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<SaveDocumentHandler> _logger;

    public SaveDocumentHandler(ApplicationDbContext dbContext, ILogger<SaveDocumentHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(ResearchPublishedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling ResearchPublishedEvent for Document {DocumentId}", notification.DocumentId);

        var session = await _dbContext.ResearchSessions.FindAsync(new object[] { notification.SessionId }, cancellationToken);
        if (session != null)
        {
            session.Status = "Published";
        }

        // Add Document
        var document = new ResearchDocumentEntity
        {
            Id = notification.DocumentId,
            SessionId = notification.SessionId,
            Title = session?.Title ?? "Untitled Document",
            WorkspaceId = notification.WorkspaceId,
            CurrentRevisionId = notification.RevisionId,
            CreatedAt = notification.OccurredAt
        };

        _dbContext.ResearchDocuments.Add(document);

        // Add DocumentRevision
        var revision = new DocumentRevisionEntity
        {
            Id = notification.RevisionId,
            DocumentId = notification.DocumentId,
            RevisionNumber = 1,
            ParentRevisionId = null,
            CreatedAt = notification.OccurredAt,
            Summary = notification.Summary,
            Markdown = $"# {document.Title}\n\n{notification.Summary}",
            Html = $"<h1>{document.Title}</h1><p>{notification.Summary}</p>",
            Json = JsonSerializer.Serialize(new { summary = notification.Summary }),
            DiffSummary = "Initial draft",
            ReasoningSessionId = notification.SessionId,
            ExecutionSnapshotId = null,
            GeneratedBy = "AI Reasoner Engine",
            GenerationType = "Regenerated"
        };

        _dbContext.DocumentRevisions.Add(revision);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class AuditLogHandler : 
    INotificationHandler<ResearchStartedEvent>,
    INotificationHandler<ResearchExecutedEvent>,
    INotificationHandler<ResearchPublishedEvent>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AuditLogHandler> _logger;

    public AuditLogHandler(ApplicationDbContext dbContext, ILogger<AuditLogHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(ResearchStartedEvent notification, CancellationToken cancellationToken)
    {
        await LogAudit("ResearchCreated", "ResearchSession", notification.SessionId.ToString(), notification.WorkspaceId, cancellationToken);
    }

    public async Task Handle(ResearchExecutedEvent notification, CancellationToken cancellationToken)
    {
        await LogAudit("EvidenceUpdated", "ResearchSession", notification.SessionId.ToString(), notification.WorkspaceId, cancellationToken);
    }

    public async Task Handle(ResearchPublishedEvent notification, CancellationToken cancellationToken)
    {
        await LogAudit("DocumentPublished", "ResearchDocument", notification.DocumentId.ToString(), notification.WorkspaceId, cancellationToken);
    }

    private async Task LogAudit(string action, string entityType, string entityId, Guid workspaceId, CancellationToken cancellationToken)
    {
        var audit = new AuditRecordEntity
        {
            Id = Guid.NewGuid(),
            Action = action,
            Actor = "AI Agent Client",
            EntityType = entityType,
            EntityId = entityId,
            OldStateHash = string.Empty,
            NewStateHash = string.Empty,
            CorrelationId = Guid.NewGuid(),
            RequestId = Guid.NewGuid().ToString(),
            UserId = "SystemUser",
            MachineName = Environment.MachineName,
            ApplicationVersion = "1.0.0",
            OccurredAt = DateTimeOffset.UtcNow
        };

        _dbContext.AuditRecords.Add(audit);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
