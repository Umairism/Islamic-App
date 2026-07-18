using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IslamicApp.Application.Research.Memory;

public interface IMemoryContextBuilder
{
    Task<MemorySelectionResult> BuildContextAsync(IEnumerable<MemoryEntry> rankedEntries, MemoryContextOptions options, CancellationToken cancellationToken);
}
