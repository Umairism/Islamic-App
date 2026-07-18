using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Events;

namespace IslamicApp.Infrastructure.Persistence.Outbox;

public class OutboxService : IOutboxService
{
    private readonly ApplicationDbContext _dbContext;

    public OutboxService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task WriteEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
            Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            OccurredAt = domainEvent.OccurredAt,
            ProcessedAt = null,
            Error = null
        };

        _dbContext.OutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
