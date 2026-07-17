using System.Collections.Generic;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Research.Interfaces;

public record KnowledgeSourceDescriptor(
    string DisplayName,
    EvidenceSource SourceType,
    int Priority,
    IReadOnlyList<string> Languages,
    string Version,
    bool SupportsSemantic,
    bool SupportsCitation,
    bool SupportsCrossReference,
    bool Enabled
);

public interface IKnowledgeSource
{
    string Name { get; }
    KnowledgeSourceDescriptor Descriptor { get; }
    IReadOnlyCollection<ISourceSearcher> Searchers { get; }
    IReadOnlyCollection<ICitationStrategy> CitationStrategies { get; }
    IReadOnlyCollection<ICrossReferenceProvider> CrossReferenceProviders { get; }
}
