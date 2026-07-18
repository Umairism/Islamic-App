using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Research.Analysis;

public class EvidenceRepository : IEvidenceRepository
{
    private readonly ISearchPipeline _searchPipeline;
    private readonly ISemanticConfiguration _semanticConfig;

    public EvidenceRepository(ISearchPipeline searchPipeline, ISemanticConfiguration semanticConfig)
    {
        _searchPipeline = searchPipeline;
        _semanticConfig = semanticConfig;
    }

    public async Task<EvidenceCorpus> GetEvidenceAsync(QueryAnalysis query, CancellationToken cancellationToken)
    {
        var request = new SearchRequest(
            Query: query.Query.Original,
            Language: query.OriginalRequest.Language,
            Sources: new HashSet<EvidenceSource> { EvidenceSource.Quran, EvidenceSource.Hadith },
            Pagination: new Pagination(1, 100), // Pull up to 100 matches to form a complete research corpus
            IncludeCrossReferences: true,
            IncludeExplanations: true,
            SemanticSearchEnabled: _semanticConfig.Features.EnableEmbeddings
        );

        var searchContext = new SearchContext(request, query);
        var finalSearchContext = await _searchPipeline.ExecuteAsync(searchContext, cancellationToken);

        var evidences = finalSearchContext.RankedCandidatesList.Select(match =>
        {
            var doc = match.Document;
            return new ResearchEvidence(
                Id: new DocumentId(doc.Id),
                Source: doc.Source,
                Reference: new ReferenceId(doc.Reference.LookupKey),
                Title: $"{(doc.Source == EvidenceSource.Quran ? "Qur'an" : doc.Source.ToString())} {doc.Reference.LookupKey}",
                Content: doc.PrimaryText,
                Topics: query.ExtractedTopics.Select(t => new TopicId(t)).ToList(),
                Language: query.DetectedLanguage,
                RetrievalScore: match.Ranking.FinalValue
            );
        }).ToList();

        var topics = evidences.SelectMany(e => e.Topics).GroupBy(t => t.Value.ToLowerInvariant()).Select(g => g.First()).ToList();
        var avgRanking = evidences.Count > 0 ? evidences.Average(e => e.RetrievalScore) : 0.0;
        var aggregateConfidenceVal = Math.Clamp(avgRanking / 100.0, 0.0, 1.0);
        var aggregateConfidence = new ConfidenceScore(aggregateConfidenceVal);

        return new EvidenceCorpus(
            Evidences: evidences,
            Topics: topics,
            Language: query.OriginalRequest.Language,
            AggregateConfidence: aggregateConfidence,
            TokenEstimate: evidences.Sum(e => e.Content.Length / 4),
            SourceCount: evidences.Select(e => e.Source).Distinct().Count(),
            AverageRanking: avgRanking,
            RetrievedAt: DateTimeOffset.UtcNow
        );
    }
}
