using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface ISourceSearcher
{
    EvidenceSource Source { get; }
    SearcherCapabilities Capabilities { get; }
    Task<List<EvidenceMatch>> SearchAsync(SearchContext context, CancellationToken cancellationToken);
}
