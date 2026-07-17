using System.Collections.Generic;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Research.Models;

public record QueryIntent(
    SearchMode Mode,
    IReadOnlySet<EvidenceSource> Sources,
    IReadOnlySet<RetrievalCapability> Capabilities,
    double Confidence
);

public record NormalizedQuery(
    string Original,
    string Normalized,
    IReadOnlyList<string> Tokens,
    IReadOnlyList<string> Stems,
    IReadOnlyList<string> ArabicRoots,
    IReadOnlyList<string> Synonyms
);

public record QueryAnalysis(
    SearchRequest OriginalRequest,
    NormalizedQuery Query,
    ResearchLanguage DetectedLanguage,
    QueryIntent Intent,
    ResearchReference? ParsedReference,
    IReadOnlyList<string> ExtractedTopics
)
{
    public bool IsReferenceLookup => Intent.Mode == SearchMode.ReferenceLookup && ParsedReference != null;
}
