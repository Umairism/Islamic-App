using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Memory;

public interface IMemoryCompressor
{
    Task<MemoryEntry> CompressAsync(ResearchResult result, CancellationToken cancellationToken);
}
