using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis;

public class EvidenceDeduplicator : IEvidenceDeduplicator
{
    public EvidenceCorpus Deduplicate(EvidenceCorpus corpus)
    {
        var originalCount = corpus.Evidences.Count;
        var uniqueEvidences = new List<ResearchEvidence>();

        // Group by Reference (e.g., surah:ayah or hadith number)
        var grouped = corpus.Evidences.GroupBy(e => e.Reference.Value.ToLowerInvariant());

        foreach (var group in grouped)
        {
            // Pick the evidence with the highest retrieval score
            var primary = group.OrderByDescending(e => e.RetrievalScore).First();

            // Consolidate topics and check languages if needed, but return a single canonical representative
            var allTopics = group.SelectMany(e => e.Topics).GroupBy(t => t.Value.ToLowerInvariant()).Select(g => g.First()).ToList();

            uniqueEvidences.Add(new ResearchEvidence(
                Id: primary.Id,
                Source: primary.Source,
                Reference: primary.Reference,
                Title: primary.Title,
                Content: primary.Content,
                Topics: allTopics,
                Language: primary.Language,
                RetrievalScore: primary.RetrievalScore
            ));
        }

        // Return a new deduplicated corpus
        var aggregateConfidenceVal = uniqueEvidences.Count > 0 ? uniqueEvidences.Average(e => e.RetrievalScore) : 0.0;
        var aggregateConfidence = new ConfidenceScore(Math.Clamp(aggregateConfidenceVal / 100.0, 0.0, 1.0));

        var dedupedCorpus = new EvidenceCorpus(
            Evidences: uniqueEvidences,
            Topics: corpus.Topics,
            Language: corpus.Language,
            AggregateConfidence: aggregateConfidence,
            TokenEstimate: uniqueEvidences.Sum(e => e.Content.Length / 4), // Simple token estimate
            SourceCount: uniqueEvidences.Count,
            AverageRanking: uniqueEvidences.Count > 0 ? uniqueEvidences.Average(e => e.RetrievalScore) : 0.0,
            RetrievedAt: corpus.RetrievedAt
        );

        return dedupedCorpus;
    }
}
