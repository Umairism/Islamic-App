using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace IslamicApp.Infrastructure.Research;

public record ResearchJob(
    Guid ResearchSessionId,
    Guid WorkspaceId,
    DateTimeOffset RequestedAt
);

public interface IResearchQueue
{
    ValueTask EnqueueAsync(ResearchJob job, CancellationToken cancellationToken = default);
    ValueTask<ResearchJob> DequeueAsync(CancellationToken cancellationToken = default);
}

public class ResearchQueue : IResearchQueue
{
    private readonly Channel<ResearchJob> _channel;

    public ResearchQueue()
    {
        // Unbounded channel to prevent blocking client requests under heavy loads
        _channel = Channel.CreateUnbounded<ResearchJob>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async ValueTask EnqueueAsync(ResearchJob job, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public async ValueTask<ResearchJob> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
