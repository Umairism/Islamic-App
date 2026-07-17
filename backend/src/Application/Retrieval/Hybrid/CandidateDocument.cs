using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Application.Retrieval.Hybrid;

public enum RetrievalMethod
{
    Lexical,
    Semantic,
    Hybrid
}

public record CandidateDocument(
    string Id,
    EvidenceSource Source,
    ResearchReference Reference,
    float Score,
    VectorMetadata Metadata,
    RetrievalMethod Method,
    KnowledgeDocument Document
);
