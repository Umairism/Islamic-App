using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace IslamicApp.Infrastructure.Research;

public class ResearchHub : Hub
{
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }
}
