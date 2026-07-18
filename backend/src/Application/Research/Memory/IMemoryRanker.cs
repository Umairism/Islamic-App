using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IslamicApp.Application.Research.Memory;

public interface IMemoryRanker
{
    Task<IReadOnlyList<MemoryEntry>> RankAsync(string query, IEnumerable<MemoryEntry> entries, CancellationToken cancellationToken);
}
