using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IslamicApp.Application.Research.Memory;

public interface IMemoryRetriever
{
    Task<IReadOnlyList<MemoryEntry>> RetrieveAsync(Guid workspaceId, string query, CancellationToken cancellationToken);
}
