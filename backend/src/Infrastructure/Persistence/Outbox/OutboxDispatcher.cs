using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IslamicApp.Application.Research.Events;

namespace IslamicApp.Infrastructure.Persistence.Outbox;

public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IServiceProvider serviceProvider, ILogger<OutboxDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxDispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing outbox messages.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        _logger.LogInformation("Processing {Count} outbox messages...", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                var eventType = ResolveEventType(message.EventType);
                if (eventType == null)
                {
                    throw new InvalidOperationException($"Could not resolve event type: {message.EventType}");
                }

                var domainEvent = JsonSerializer.Deserialize(message.Content, eventType);
                if (domainEvent == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize event: {message.EventType}");
                }

                // Publish domain event via MediatR in-process dispatcher
                await mediator.Publish(domainEvent, cancellationToken);

                message.ProcessedAt = DateTimeOffset.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId} ({EventType})", message.Id, message.EventType);
                message.Error = ex.ToString();
                // We mark it processed to avoid blocking the queue, but log the error details in the table
                message.ProcessedAt = DateTimeOffset.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Type? ResolveEventType(string eventTypeName)
    {
        if (eventTypeName.Contains("ResearchStartedEvent")) return typeof(ResearchStartedEvent);
        if (eventTypeName.Contains("ResearchExecutedEvent")) return typeof(ResearchExecutedEvent);
        if (eventTypeName.Contains("ResearchValidatedEvent")) return typeof(ResearchValidatedEvent);
        if (eventTypeName.Contains("ResearchPublishedEvent")) return typeof(ResearchPublishedEvent);
        if (eventTypeName.Contains("DocumentCreatedEvent")) return typeof(DocumentCreatedEvent);
        if (eventTypeName.Contains("RevisionCreatedEvent")) return typeof(RevisionCreatedEvent);
        if (eventTypeName.Contains("WorkspaceCreatedEvent")) return typeof(WorkspaceCreatedEvent);
        if (eventTypeName.Contains("WorkspaceExportedEvent")) return typeof(WorkspaceExportedEvent);
        if (eventTypeName.Contains("BookmarkAddedEvent")) return typeof(BookmarkAddedEvent);
        if (eventTypeName.Contains("NoteCreatedEvent")) return typeof(NoteCreatedEvent);
        if (eventTypeName.Contains("ValidationFailedEvent")) return typeof(ValidationFailedEvent);

        return Type.GetType(eventTypeName);
    }
}
