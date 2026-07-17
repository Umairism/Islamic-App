using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface ISourceSearcher
{
    EvidenceSource Source { get; }
    Task<IReadOnlyList<KnowledgeMatch>> SearchAsync(SearchContext context, CancellationToken cancellationToken);
}
