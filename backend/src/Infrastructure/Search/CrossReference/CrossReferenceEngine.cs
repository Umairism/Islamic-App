using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.CrossReference;

public class CrossReferenceEngine : ICrossReferenceEngine
{
    private readonly IEnumerable<ICrossReferenceProvider> _providers;

    public CrossReferenceEngine(IEnumerable<ICrossReferenceProvider> providers)
    {
        _providers = providers;
    }

    public async Task<List<CrossReferenceItem>> ResolveReferencesAsync(EvidenceSource source, string reference, CancellationToken cancellationToken)
    {
        var items = new List<CrossReferenceItem>();
        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var results = await provider.GetReferencesAsync(reference, cancellationToken);
            if (results != null)
            {
                items.AddRange(results);
            }
        }
        return items.Distinct().ToList();
    }
}
