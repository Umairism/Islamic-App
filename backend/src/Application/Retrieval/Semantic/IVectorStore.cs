using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Retrieval.Semantic;

public enum VectorCollection
{
    Quran,
    Hadith,
    Tafsir,
    Dictionary,
    Fatwa,
    Ontology
}

public record VectorDocument(
    string Id,
    EvidenceSource Source,
    ResearchReference Reference,
    float Score,
    VectorMetadata Metadata
);

public interface IVectorStore
{
    Task UpsertAsync(
        VectorCollection collection,
        string id,
        float[] vector,
        EvidenceSource source,
        ResearchReference reference,
        VectorMetadata metadata,
        CancellationToken cancellationToken);

    Task<List<VectorDocument>> SearchAsync(
        VectorCollection collection,
        float[] vector,
        int limit,
        ISimilarityMetric similarityMetric,
        CancellationToken cancellationToken);
}
