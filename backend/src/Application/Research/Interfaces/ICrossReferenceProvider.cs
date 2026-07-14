using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface ICrossReferenceProvider
{
    EvidenceSource Source { get; }
    Task<List<CrossReferenceItem>> GetReferencesAsync(string reference, CancellationToken cancellationToken);
}
