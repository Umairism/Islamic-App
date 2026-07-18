using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Events;

namespace IslamicApp.Application.Research.Analysis;

public interface IOutboxService
{
    Task WriteEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
