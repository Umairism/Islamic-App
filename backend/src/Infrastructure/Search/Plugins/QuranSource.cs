using System.Collections.Generic;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Infrastructure.Search.Citation;
using IslamicApp.Infrastructure.Search.CrossReference;

namespace IslamicApp.Infrastructure.Search.Plugins;

public class QuranSource : IKnowledgeSource
{
    public string Name => "Quran";

    public KnowledgeSourceDescriptor Descriptor => new(
        DisplayName: "The Holy Qur'an",
        SourceType: EvidenceSource.Quran,
        Priority: 1,
        Languages: new List<string> { "ar", "en", "ur" },
        Version: "3.1.2",
        SupportsSemantic: false,
        SupportsCitation: true,
        SupportsCrossReference: true,
        Enabled: true
    );

    public IReadOnlyCollection<ISourceSearcher> Searchers { get; }
    public IReadOnlyCollection<ICitationStrategy> CitationStrategies { get; }
    public IReadOnlyCollection<ICrossReferenceProvider> CrossReferenceProviders { get; }

    public QuranSource(
        QuranSearcher searcher,
        QuranCitationStrategy citationStrategy,
        QuranCrossReferenceProvider crossRefProvider)
    {
        Searchers = new List<ISourceSearcher> { searcher };
        CitationStrategies = new List<ICitationStrategy> { citationStrategy };
        CrossReferenceProviders = new List<ICrossReferenceProvider> { crossRefProvider };
    }
}
