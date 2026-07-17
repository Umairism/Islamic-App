using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Semantic.Vector;

public class InMemoryVectorStore : IVectorStore
{
    private class StoredVector
    {
        public string Id { get; set; } = string.Empty;
        public float[] Vector { get; set; } = [];
        public EvidenceSource Source { get; set; }
        public ResearchReference Reference { get; set; } = null!;
        public VectorMetadata Metadata { get; set; } = null!;
    }

    private readonly ConcurrentDictionary<VectorCollection, ConcurrentDictionary<string, StoredVector>> _store = new();

    public Task UpsertAsync(
        VectorCollection collection,
        string id,
        float[] vector,
        EvidenceSource source,
        ResearchReference reference,
        VectorMetadata metadata,
        CancellationToken cancellationToken)
    {
        var collectionStore = _store.GetOrAdd(collection, _ => new ConcurrentDictionary<string, StoredVector>());
        var doc = new StoredVector
        {
            Id = id,
            Vector = vector,
            Source = source,
            Reference = reference,
            Metadata = metadata
        };
        collectionStore[id] = doc;
        return Task.CompletedTask;
    }

    public Task<List<VectorDocument>> SearchAsync(
        VectorCollection collection,
        float[] vector,
        int limit,
        ISimilarityMetric similarityMetric,
        CancellationToken cancellationToken)
    {
        if (!_store.TryGetValue(collection, out var collectionStore))
        {
            return Task.FromResult(new List<VectorDocument>());
        }

        var results = collectionStore.Values
            .Select(doc => new VectorDocument(
                doc.Id,
                doc.Source,
                doc.Reference,
                (float)similarityMetric.Calculate(vector, doc.Vector),
                doc.Metadata
            ))
            .OrderByDescending(d => d.Score)
            .Take(limit)
            .ToList();

        return Task.FromResult(results);
    }
}
