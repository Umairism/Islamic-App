using System.Collections.Generic;

namespace IslamicApp.Application.Semantic.Query;

public record SemanticQuery(
    string RawQuery,
    IReadOnlyList<string> ExpandedTokens,
    IReadOnlyList<string> Concepts,
    IReadOnlyList<string> ArabicRoots,
    double Confidence
);
