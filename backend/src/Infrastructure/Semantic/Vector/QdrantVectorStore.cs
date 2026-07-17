using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Semantic.Vector;

public class QdrantVectorStore : IVectorStore
{
    private readonly QdrantClient? _client;

    public QdrantVectorStore(string host = "localhost", int port = 6334)
    {
        try
        {
            _client = new QdrantClient(host, port);
        }
        catch
        {
            _client = null;
        }
    }

    public async Task UpsertAsync(
        VectorCollection collection,
        string id,
        float[] vector,
        EvidenceSource source,
        ResearchReference reference,
        VectorMetadata metadata,
        CancellationToken cancellationToken)
    {
        if (_client == null) return;

        string collectionName = collection.ToString().ToLowerInvariant();

        // Ensure collection exists
        try
        {
            var collections = await _client.ListCollectionsAsync(cancellationToken);
            if (!collections.Contains(collectionName))
            {
                await _client.CreateCollectionAsync(collectionName, new VectorParams
                {
                    Size = (ulong)vector.Length,
                    Distance = Distance.Cosine
                }, cancellationToken: cancellationToken);
            }
        }
        catch
        {
            // Ignore connection/creation errors in fallback mode
            return;
        }

        // Map ID to UUID format for Qdrant point struct
        Guid pointId = GeneratePointId(id);

        var point = new PointStruct
        {
            Id = pointId,
            Vectors = vector
        };

        point.Payload.Add("id", id);
        point.Payload.Add("source", source.ToString());
        point.Payload.Add("reference", reference.LookupKey);
        point.Payload.Add("datasetId", metadata.Storage.DatasetId);
        point.Payload.Add("checksum", metadata.Storage.Checksum);
        point.Payload.Add("importedAt", metadata.Storage.ImportedAt.ToString("o"));
        point.Payload.Add("language", metadata.Retrieval.Language.ToString());

        var topicsList = new Value();
        topicsList.ListValue = new ListValue();
        foreach (var t in metadata.Retrieval.Topics) topicsList.ListValue.Values.Add(t);
        point.Payload.Add("topics", topicsList);

        var keywordsList = new Value();
        keywordsList.ListValue = new ListValue();
        foreach (var k in metadata.Retrieval.Keywords) keywordsList.ListValue.Values.Add(k);
        point.Payload.Add("keywords", keywordsList);

        await _client.UpsertAsync(collectionName, new[] { point }, cancellationToken: cancellationToken);
    }

    public async Task<List<VectorDocument>> SearchAsync(
        VectorCollection collection,
        float[] vector,
        int limit,
        ISimilarityMetric similarityMetric,
        CancellationToken cancellationToken)
    {
        if (_client == null) return new List<VectorDocument>();

        string collectionName = collection.ToString().ToLowerInvariant();

        try
        {
            var results = await _client.SearchAsync(
                collectionName,
                vector,
                limit: (ulong)limit,
                cancellationToken: cancellationToken
            );

            var documents = new List<VectorDocument>();
            foreach (var hit in results)
            {
                var payload = hit.Payload;
                string id = payload.TryGetValue("id", out var idVal) ? idVal.StringValue : hit.Id.ToString();
                
                string sourceStr = payload.TryGetValue("source", out var srcVal) ? srcVal.StringValue : "Quran";
                EvidenceSource source = Enum.TryParse<EvidenceSource>(sourceStr, out var src) ? src : EvidenceSource.Quran;

                string refStr = payload.TryGetValue("reference", out var refVal) ? refVal.StringValue : "1:1";
                ResearchReference reference = ParseReference(source, refStr);

                string datasetId = payload.TryGetValue("datasetId", out var dsVal) ? dsVal.StringValue : string.Empty;
                string checksum = payload.TryGetValue("checksum", out var chkVal) ? chkVal.StringValue : string.Empty;
                DateTime importedAt = payload.TryGetValue("importedAt", out var impVal) && DateTime.TryParse(impVal.StringValue, out var dt) ? dt : DateTime.UtcNow;

                string langStr = payload.TryGetValue("language", out var langVal) ? langVal.StringValue : "Auto";
                ResearchLanguage language = Enum.TryParse<ResearchLanguage>(langStr, out var lang) ? lang : ResearchLanguage.Auto;

                var topics = payload.TryGetValue("topics", out var topVal) && topVal.KindCase == Value.KindOneofCase.ListValue
                    ? topVal.ListValue.Values.Select(v => v.StringValue).ToList()
                    : new List<string>();

                var keywords = payload.TryGetValue("keywords", out var keyVal) && keyVal.KindCase == Value.KindOneofCase.ListValue
                    ? keyVal.ListValue.Values.Select(v => v.StringValue).ToList()
                    : new List<string>();

                var metadata = new VectorMetadata(
                    new VectorStorageMetadata(datasetId, checksum, importedAt),
                    new VectorRetrievalMetadata(language, topics, keywords)
                );

                documents.Add(new VectorDocument(id, source, reference, hit.Score, metadata));
            }

            return documents;
        }
        catch
        {
            return new List<VectorDocument>();
        }
    }

    private Guid GeneratePointId(string id)
    {
        // Simple deterministic Guid from string hash
        using var md5 = System.Security.Cryptography.MD5.Create();
        byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(id));
        return new Guid(hash);
    }

    private ResearchReference ParseReference(EvidenceSource source, string key)
    {
        if (source == EvidenceSource.Hadith)
        {
            var parts = key.Split(':');
            string col = parts.Length > 0 ? parts[0] : "Sahih al-Bukhari";
            int hadithNum = parts.Length > 1 && int.TryParse(parts[1], out var n) ? n : 1;
            return new HadithReference(col, 1, hadithNum);
        }
        else
        {
            var parts = key.Split(':');
            int surah = parts.Length > 0 && int.TryParse(parts[0], out var s) ? s : 1;
            int ayah = parts.Length > 1 && int.TryParse(parts[1], out var a) ? a : 1;
            return new QuranReference(surah, ayah);
        }
    }
}
