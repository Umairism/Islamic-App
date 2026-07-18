using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using IslamicApp.Application.Research.Events;
using IslamicApp.Infrastructure.Research;

namespace IslamicApp.Infrastructure.Persistence.EventHandlers;

public record ResearchProgressDto(
    Guid SessionId,
    string Stage,
    int Progress,
    string Message
);

public class ResearchStageCompletedHandler : 
    INotificationHandler<ResearchStageCompletedEvent>,
    INotificationHandler<ResearchSessionStartedEvent>,
    INotificationHandler<ResearchSessionCompletedEvent>,
    INotificationHandler<ResearchSessionCancelledEvent>,
    INotificationHandler<ResearchSessionFailedEvent>
{
    private readonly IHubContext<ResearchHub> _hubContext;

    public ResearchStageCompletedHandler(IHubContext<ResearchHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task Handle(ResearchStageCompletedEvent notification, CancellationToken cancellationToken)
    {
        var dto = new ResearchProgressDto(
            notification.SessionId,
            notification.Stage,
            notification.Progress,
            notification.Message
        );

        await _hubContext.Clients.Group(notification.SessionId.ToString())
            .SendAsync("OnStageUpdated", dto, cancellationToken);
    }

    public async Task Handle(ResearchSessionStartedEvent notification, CancellationToken cancellationToken)
    {
        var dto = new ResearchProgressDto(notification.SessionId, "Started", 5, "Research session started.");
        await _hubContext.Clients.Group(notification.SessionId.ToString()).SendAsync("OnStageUpdated", dto, cancellationToken);
    }

    public async Task Handle(ResearchSessionCompletedEvent notification, CancellationToken cancellationToken)
    {
        var dto = new ResearchProgressDto(notification.SessionId, "Completed", 100, "Research session completed successfully.");
        await _hubContext.Clients.Group(notification.SessionId.ToString()).SendAsync("OnStageUpdated", dto, cancellationToken);
    }

    public async Task Handle(ResearchSessionCancelledEvent notification, CancellationToken cancellationToken)
    {
        var dto = new ResearchProgressDto(notification.SessionId, "Cancelled", 0, "Research session was cancelled.");
        await _hubContext.Clients.Group(notification.SessionId.ToString()).SendAsync("OnStageUpdated", dto, cancellationToken);
    }

    public async Task Handle(ResearchSessionFailedEvent notification, CancellationToken cancellationToken)
    {
        var dto = new ResearchProgressDto(notification.SessionId, "Failed", 0, $"Research failed: {notification.ErrorMessage}");
        await _hubContext.Clients.Group(notification.SessionId.ToString()).SendAsync("OnStageUpdated", dto, cancellationToken);
    }
}
