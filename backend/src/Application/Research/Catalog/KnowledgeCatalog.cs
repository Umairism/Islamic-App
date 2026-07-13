using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Interfaces;

namespace IslamicApp.Application.Research.Catalog;

public class KnowledgeCatalog
{
    private readonly List<KnowledgeSourceDescriptor> _descriptors = new();

    public IReadOnlyList<ISourceSearcher> Searchers { get; }
    public IReadOnlyList<ICitationStrategy> CitationStrategies { get; }
    public IReadOnlyList<KnowledgeSourceDescriptor> Sources => _descriptors;

    public KnowledgeCatalog(
        IEnumerable<ISourceSearcher> searchers,
        IEnumerable<ICitationStrategy> strategies)
    {
        Searchers = searchers.ToList();
        CitationStrategies = strategies.ToList();

        // Compose source registry descriptors
        _descriptors.Add(new KnowledgeSourceDescriptor(
            Source: Enums.EvidenceSource.Quran,
            DisplayName: "Qur'an",
            Version: "1.0",
            Languages: new List<string> { "ar", "en", "ur" },
            Priority: 100,
            Enabled: true,
            SupportsSearch: true,
            SupportsCitation: true
        ));

        _descriptors.Add(new KnowledgeSourceDescriptor(
            Source: Enums.EvidenceSource.Hadith,
            DisplayName: "Hadith Collections",
            Version: "1.0",
            Languages: new List<string> { "ar", "en" },
            Priority: 90,
            Enabled: true,
            SupportsSearch: true,
            SupportsCitation: true
        ));
    }
}
