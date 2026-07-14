using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Interfaces;

public interface ICrossReferenceEngine
{
    Task<List<CrossReferenceItem>> ResolveReferencesAsync(EvidenceSource source, string reference, CancellationToken cancellationToken);
}
