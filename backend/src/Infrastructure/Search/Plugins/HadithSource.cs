using System.Collections.Generic;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Infrastructure.Search.Citation;
using IslamicApp.Infrastructure.Search.CrossReference;

namespace IslamicApp.Infrastructure.Search.Plugins;

public class HadithSource : IKnowledgeSource
{
    public string Name => "Hadith";

    public KnowledgeSourceDescriptor Descriptor => new(
        DisplayName: "Hadith Collections",
        SourceType: EvidenceSource.Hadith,
        Priority: 2,
        Languages: new List<string> { "ar", "en" },
        Version: "1.0.0",
        SupportsSemantic: false,
        SupportsCitation: true,
        SupportsCrossReference: true,
        Enabled: true
    );

    public IReadOnlyCollection<ISourceSearcher> Searchers { get; }
    public IReadOnlyCollection<ICitationStrategy> CitationStrategies { get; }
    public IReadOnlyCollection<ICrossReferenceProvider> CrossReferenceProviders { get; }

    public HadithSource(
        HadithSearcher searcher,
        HadithCitationStrategy citationStrategy,
        HadithCrossReferenceProvider crossRefProvider)
    {
        Searchers = new List<ISourceSearcher> { searcher };
        CitationStrategies = new List<ICitationStrategy> { citationStrategy };
        CrossReferenceProviders = new List<ICrossReferenceProvider> { crossRefProvider };
    }
}
