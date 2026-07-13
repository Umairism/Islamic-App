using System;
using System.Linq;
using IslamicApp.Application.Research.Catalog;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.Citation;

public class CitationFormatter : ICitationFormatter
{
    private readonly KnowledgeCatalog _catalog;

    public CitationFormatter(KnowledgeCatalog catalog)
    {
        _catalog = catalog;
    }

    public string Format(KnowledgeIdentifier identifier, EvidenceMetadata metadata)
    {
        if (identifier == null) return string.Empty;

        var strategy = _catalog.CitationStrategies.FirstOrDefault(s => s.Source == identifier.Source);
        if (strategy != null)
        {
            return strategy.Format(identifier, metadata);
        }

        // Fallback default citation
        return $"{identifier.Source} {identifier.Collection} {identifier.Book}:{identifier.VerseOrHadithNumber}";
    }
}
