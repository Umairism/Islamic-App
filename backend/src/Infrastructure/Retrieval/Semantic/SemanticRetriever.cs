using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Retrieval.Diagnostics;
using IslamicApp.Application.Retrieval.Hybrid;
using IslamicApp.Application.Retrieval.Lexical;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Retrieval.Semantic;

public class SemanticRetriever : ISemanticRetriever
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingPipeline _embeddingPipeline;
    private readonly ISimilarityMetric _similarityMetric;
    private readonly ILexicalRetriever _lexicalRetriever;

    public SemanticRetriever(
        IVectorStore vectorStore,
        IEmbeddingPipeline embeddingPipeline,
        ISimilarityMetric similarityMetric,
        ILexicalRetriever lexicalRetriever)
    {
        _vectorStore = vectorStore;
        _embeddingPipeline = embeddingPipeline;
        _similarityMetric = similarityMetric;
        _lexicalRetriever = lexicalRetriever;
    }

    public async Task<List<CandidateDocument>> RetrieveAsync(RetrievalContext context)
    {
        var candidates = new List<CandidateDocument>();
        if (context.Query.Query.Original == null) return candidates;

        // Generate embedding via pipeline
        var embeddingReq = new EmbeddingRequest(context.Query.Query.Original, context.Query.DetectedLanguage);
        var embeddingCtx = await _embeddingPipeline.ProcessAsync(embeddingReq, context.CancellationToken);
        if (embeddingCtx.Vector == null) return candidates;

        // Search Quran and/or Hadith collections based on target sources
        var collectionsToSearch = new List<VectorCollection>();
        if (context.Query.Intent.Sources.Contains(EvidenceSource.Quran))
        {
            collectionsToSearch.Add(VectorCollection.Quran);
        }
        if (context.Query.Intent.Sources.Contains(EvidenceSource.Hadith))
        {
            collectionsToSearch.Add(VectorCollection.Hadith);
        }

        if (collectionsToSearch.Count == 0)
        {
            collectionsToSearch.Add(VectorCollection.Quran);
            collectionsToSearch.Add(VectorCollection.Hadith);
        }

        int searchLimit = context.Query.OriginalRequest.Pagination.PageSize * 2;

        foreach (var col in collectionsToSearch)
        {
            var vectorDocs = await _vectorStore.SearchAsync(
                col,
                embeddingCtx.Vector,
                searchLimit,
                _similarityMetric,
                context.CancellationToken);

            foreach (var doc in vectorDocs)
            {
                // Resolve full document details from database / cache
                var fullDoc = await _lexicalRetriever.GetDocumentByReferenceAsync(doc.Reference, context.CancellationToken);
                if (fullDoc != null)
                {
                    candidates.Add(new CandidateDocument(
                        Id: doc.Id,
                        Source: doc.Source,
                        Reference: doc.Reference,
                        Score: doc.Score,
                        Metadata: doc.Metadata,
                        Method: RetrievalMethod.Semantic,
                        Document: fullDoc
                    ));
                }
            }
        }

        return candidates;
    }
}
