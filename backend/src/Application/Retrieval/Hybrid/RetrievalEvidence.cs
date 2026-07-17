using System.Collections.Generic;

namespace IslamicApp.Application.Retrieval.Hybrid;

public record SemanticEvidence(
    double SemanticSimilarity,
    IReadOnlyList<string> MatchedConcepts,
    IReadOnlyList<string> MatchedRoots,
    IReadOnlyList<string> ExpandedTerms
);

public record RetrievalEvidence(
    RetrievalMethod Method,
    float Similarity,
    SemanticEvidence Semantic,
    string Explanation
);
