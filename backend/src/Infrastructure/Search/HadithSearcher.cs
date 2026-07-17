using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Retrieval.Lexical;

using IslamicApp.Application.Retrieval.Hybrid;

namespace IslamicApp.Infrastructure.Search;

public class HadithSearcher : ISourceSearcher
{
    private readonly ILexicalRetriever _retriever;

    public EvidenceSource Source => EvidenceSource.Hadith;

    public HadithSearcher(ILexicalRetriever retriever)
    {
        _retriever = retriever;
    }

    public async Task<IReadOnlyList<KnowledgeMatch>> SearchAsync(SearchContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var defaultEvidence = new RetrievalEvidence(
            Method: RetrievalMethod.Lexical,
            Similarity: 0.0f,
            Semantic: new SemanticEvidence(0.0f, new List<string>(), new List<string>(), new List<string>()),
            Explanation: "Lexical search retrieval"
        );

        // 1. Reference Lookup Routing Path
        if (context.Analysis.IsReferenceLookup && context.Analysis.ParsedReference is HadithReference href)
        {
            var doc = await _retriever.GetDocumentByReferenceAsync(href, cancellationToken);
            if (doc != null)
            {
                var scoreObj = new RankingScore(0.0, new List<RankingContribution>());
                return new List<KnowledgeMatch> { new(doc, new List<string>(), scoreObj, defaultEvidence) };
            }
            return Array.Empty<KnowledgeMatch>();
        }

        // 2. Keyword/Topic Lexical Query Path
        var parameters = new LexicalSearchParameters(
            Query: context.Analysis.Query,
            Language: context.Analysis.DetectedLanguage,
            TargetReference: context.Analysis.ParsedReference,
            TargetSources: new HashSet<EvidenceSource> { EvidenceSource.Hadith },
            Pagination: context.Request.Pagination
        );

        var docs = await _retriever.QueryLexicalDocumentsAsync(parameters, cancellationToken);

        return docs.Select(doc => new KnowledgeMatch(
            Document: doc,
            MatchedTokens: context.Analysis.Query.Tokens,
            Ranking: new RankingScore(0.0, new List<RankingContribution>()),
            Evidence: defaultEvidence
        )).ToList();
    }
}
