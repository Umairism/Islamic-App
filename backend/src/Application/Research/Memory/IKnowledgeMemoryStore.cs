using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IslamicApp.Application.Research.Memory;

public interface IKnowledgeMemoryStore
{
    Task StoreAsync(MemoryEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<MemoryEntry>> GetWorkspaceMemoriesAsync(Guid workspaceId, CancellationToken cancellationToken);
    Task InvalidateMemoryAsync(Guid workspaceId, string query, MemoryInvalidationReason reason, CancellationToken cancellationToken);
}
