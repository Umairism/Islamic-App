using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Retrieval.Diagnostics;
using IslamicApp.Application.Retrieval.Hybrid;
using IslamicApp.Application.Retrieval.Lexical;
using IslamicApp.Application.Retrieval.Semantic;
using IslamicApp.Application.Retrieval.Policies;

namespace IslamicApp.Infrastructure.Retrieval.Hybrid;

public class RetrievalOrchestrator : IRetrievalOrchestrator
{
    private readonly ILexicalRetriever _lexicalRetriever;
    private readonly ISemanticRetriever _semanticRetriever;
    private readonly ISemanticConfiguration _config;
    private readonly IFusionStrategy _defaultFusionStrategy;

    public RetrievalOrchestrator(
        ILexicalRetriever lexicalRetriever,
        ISemanticRetriever semanticRetriever,
        ISemanticConfiguration config,
        IFusionStrategy defaultFusionStrategy)
    {
        _lexicalRetriever = lexicalRetriever;
        _semanticRetriever = semanticRetriever;
        _config = config;
        _defaultFusionStrategy = defaultFusionStrategy;
    }

    public async Task<(List<CandidateDocument> Candidates, RetrievalContext UpdatedContext)> RetrieveCandidatesAsync(
        RetrievalContext context)
    {
        var activePolicy = ResolvePolicy(context);
        var events = context.Events;

        List<CandidateDocument> lexicalCandidates = new();
        List<CandidateDocument> semanticCandidates = new();

        bool runLexical = activePolicy == SemanticPolicy.LexicalOnly ||
                         activePolicy == SemanticPolicy.Hybrid ||
                         activePolicy == SemanticPolicy.HybridPreferLexical ||
                         activePolicy == SemanticPolicy.HybridPreferSemantic ||
                         activePolicy == SemanticPolicy.Auto;

        bool runSemantic = _config.Features.EnableEmbeddings &&
                          (activePolicy == SemanticPolicy.SemanticOnly ||
                           activePolicy == SemanticPolicy.Hybrid ||
                           activePolicy == SemanticPolicy.HybridPreferLexical ||
                           activePolicy == SemanticPolicy.HybridPreferSemantic ||
                           activePolicy == SemanticPolicy.Auto);

        // 1. Run Lexical Retrieval
        if (runLexical)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var queryTokens = context.Query.Query.Tokens;
                var targetRef = context.Query.ParsedReference;
                
                var lexParams = new LexicalSearchParameters(
                    Query: context.Query.Query,
                    Language: context.Query.DetectedLanguage,
                    TargetReference: targetRef,
                    TargetSources: context.Query.Intent.Sources,
                    Pagination: context.Query.OriginalRequest.Pagination
                );

                var docs = await _lexicalRetriever.QueryLexicalDocumentsAsync(lexParams, context.CancellationToken);
                
                // Map to candidate documents
                lexicalCandidates = docs.Select((doc, i) => new CandidateDocument(
                    Id: doc.Id,
                    Source: doc.Source,
                    Reference: doc.Reference,
                    Score: (float)(1.0 - (i * 0.05)), // Pseudo-score mapped from index ranking
                    Metadata: new VectorMetadata(
                        new VectorStorageMetadata(doc.DatasetId, "chk", DateTime.UtcNow),
                        new VectorRetrievalMetadata(context.Query.DetectedLanguage, new List<string>(), new List<string>())
                    ),
                    Method: RetrievalMethod.Lexical,
                    Document: doc
                )).ToList();

                sw.Stop();
                var metadata = new Dictionary<string, string>
                {
                    ["Count"] = lexicalCandidates.Count.ToString(),
                    ["AverageScore"] = lexicalCandidates.Count > 0 ? lexicalCandidates.Average(c => c.Score).ToString("F3") : "0"
                };
                events = events.Add(new PipelineEvent("LexicalSearch", "QueryLexicalDocumentsAsync", sw.Elapsed, metadata));
            }
            catch (Exception ex)
            {
                sw.Stop();
                events = events.Add(new PipelineEvent("LexicalSearch", $"Failure: {ex.Message}", sw.Elapsed, new Dictionary<string, string>()));
            }
        }

        // 2. Run Semantic Retrieval
        if (runSemantic)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                semanticCandidates = await _semanticRetriever.RetrieveAsync(context);
                sw.Stop();

                var metadata = new Dictionary<string, string>
                {
                    ["Count"] = semanticCandidates.Count.ToString(),
                    ["AverageScore"] = semanticCandidates.Count > 0 ? semanticCandidates.Average(c => c.Score).ToString("F3") : "0"
                };
                events = events.Add(new PipelineEvent("SemanticSearch", "RetrieveAsync", sw.Elapsed, metadata));
            }
            catch (Exception ex)
            {
                sw.Stop();
                events = events.Add(new PipelineEvent("SemanticSearch", $"Failure: {ex.Message}", sw.Elapsed, new Dictionary<string, string>()));
            }
        }

        // 3. Fusion Strategy
        List<CandidateDocument> finalCandidates = new();
        if (runLexical && runSemantic)
        {
            var sw = Stopwatch.StartNew();
            IFusionStrategy strategy = _defaultFusionStrategy;
            if (activePolicy == SemanticPolicy.HybridPreferSemantic)
            {
                strategy = new WeightedFusion(0.2, 0.8);
            }
            else if (activePolicy == SemanticPolicy.HybridPreferLexical)
            {
                strategy = new WeightedFusion(0.8, 0.2);
            }

            finalCandidates = strategy.Fuse(lexicalCandidates, semanticCandidates);
            sw.Stop();

            var metadata = new Dictionary<string, string>
            {
                ["Strategy"] = strategy.GetType().Name,
                ["Count"] = finalCandidates.Count.ToString()
            };
            events = events.Add(new PipelineEvent("Fusion", "Fuse", sw.Elapsed, metadata));
        }
        else if (runLexical)
        {
            finalCandidates = lexicalCandidates;
        }
        else if (runSemantic)
        {
            finalCandidates = semanticCandidates;
        }

        var updatedContext = context with { Events = events };
        return (finalCandidates, updatedContext);
    }

    private SemanticPolicy ResolvePolicy(RetrievalContext context)
    {
        if (context.Policy != SemanticPolicy.Adaptive)
        {
            return context.Policy;
        }

        // Adaptive routing logic:
        // 1. Exact reference matches go straight to Lexical
        if (context.Query.IsReferenceLookup)
        {
            return SemanticPolicy.LexicalOnly;
        }

        // 2. Query containing Arabic characters runs Hybrid search
        string raw = context.Query.OriginalRequest.Query ?? string.Empty;
        bool hasArabic = raw.Any(c => c >= 0x0600 && c <= 0x06FF);
        if (hasArabic)
        {
            return SemanticPolicy.Hybrid;
        }

        // 3. Hadith collection queries match Lexical mostly
        if (raw.StartsWith("hadith", StringComparison.OrdinalIgnoreCase) || 
            raw.StartsWith("bukhari", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("muslim", StringComparison.OrdinalIgnoreCase))
        {
            return SemanticPolicy.LexicalOnly;
        }

        // 4. Topic expansions match HybridPreferSemantic
        if (context.Query.Query.Synonyms.Count > 0)
        {
            return SemanticPolicy.HybridPreferSemantic;
        }

        return SemanticPolicy.Auto;
    }
}
