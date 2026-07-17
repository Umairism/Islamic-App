using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search.Retrieval;

public class RetrievalOrchestrator : IRetrievalOrchestrator
{
    private readonly ILexicalRetriever _lexicalRetriever;
    private readonly IEnumerable<IKnowledgeSource> _sources;

    public RetrievalOrchestrator(ILexicalRetriever lexicalRetriever, IEnumerable<IKnowledgeSource> sources)
    {
        _lexicalRetriever = lexicalRetriever;
        _sources = sources;
    }

    public async Task<IReadOnlyList<KnowledgeMatch>> RetrieveMatchesAsync(
        QueryAnalysis analysis,
        CancellationToken cancellationToken)
    {
        if (analysis == null) throw new ArgumentNullException(nameof(analysis));

        var matches = new List<KnowledgeMatch>();

        // 1. Reference Lookup Routing Path
        if (analysis.IsReferenceLookup && analysis.ParsedReference != null)
        {
            var doc = await _lexicalRetriever.GetDocumentByReferenceAsync(analysis.ParsedReference, cancellationToken);
            if (doc != null)
            {
                var ranking = new RankingScore(0.0, new List<RankingContribution>());
                matches.Add(new KnowledgeMatch(doc, new List<string>(), ranking));
            }
            return matches;
        }

        // 2. Keyword/Topic Search Routing Path
        var activeSources = _sources.Where(s => s.Descriptor.Enabled).ToList();
        var targetSources = analysis.Intent.Sources.Intersect(activeSources.Select(s => s.Descriptor.SourceType)).ToHashSet();

        if (targetSources.Count == 0)
        {
            // Default to all active sources if none specified
            targetSources = activeSources.Select(s => s.Descriptor.SourceType).ToHashSet();
        }

        var retrievalParams = new LexicalSearchParameters(
            Query: analysis.Query,
            Language: analysis.DetectedLanguage,
            TargetReference: analysis.ParsedReference,
            TargetSources: targetSources,
            Pagination: analysis.OriginalRequest.Pagination
        );

        var docs = await _lexicalRetriever.QueryLexicalDocumentsAsync(retrievalParams, cancellationToken);
        foreach (var doc in docs)
        {
            var ranking = new RankingScore(0.0, new List<RankingContribution>());
            matches.Add(new KnowledgeMatch(doc, new List<string>(), ranking));
        }

        return matches;
    }
}
