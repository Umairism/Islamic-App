using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Retrieval.Hybrid;

namespace IslamicApp.Infrastructure.Search;

public class RankingEngine : IRankingEngine
{
    private readonly IRankingWeightsProvider _weightsProvider;

    public RankingEngine(IRankingWeightsProvider weightsProvider)
    {
        _weightsProvider = weightsProvider;
    }

    public SearchContext Rank(SearchContext context)
    {
        if (context == null || (context.CandidatesList.Count == 0 && context.RetrievedCandidatesList.Count == 0))
            return context ?? null!;

        var rankedList = new List<KnowledgeMatch>();

        var candidatesToScrutinize = new List<(KnowledgeDocument Document, float RetrievalScore, RetrievalMethod Method)>();

        if (context.RetrievedCandidatesList.Count > 0)
        {
            foreach (var cand in context.RetrievedCandidatesList)
            {
                candidatesToScrutinize.Add((cand.Document, cand.Score, cand.Method));
            }
        }
        else
        {
            foreach (var cand in context.CandidatesList)
            {
                var doc = cand.Document;
                candidatesToScrutinize.Add((doc, (float)cand.Ranking.FinalValue, RetrievalMethod.Lexical));
            }
        }

        foreach (var item in candidatesToScrutinize)
        {
            var document = item.Document;
            var contributions = new List<RankingContribution>();
            double finalScore = 0;

            // 1. Exact Reference Match Check
            if (context.Analysis.ParsedReference != null && 
                IsReferenceMatch(document.Source, document.Reference, context.Analysis.ParsedReference))
            {
                bool isAlias = !context.Analysis.OriginalRequest.Query.Trim().Equals(context.Analysis.ParsedReference.LookupKey, StringComparison.OrdinalIgnoreCase) &&
                               !context.Analysis.OriginalRequest.Query.Trim().Equals($"{context.Analysis.ParsedReference.Source} {context.Analysis.ParsedReference.LookupKey}", StringComparison.OrdinalIgnoreCase) &&
                               !context.Analysis.OriginalRequest.Query.Trim().Contains(":");

                double weight = _weightsProvider.GetWeight(isAlias ? "Alias" : "Reference");
                double contribVal = weight * 1.0;
                contributions.Add(new RankingContribution(
                    Factor: RankingFactor.ExactReference,
                    Weight: weight,
                    Value: 1.0,
                    Contribution: contribVal
                ));
                finalScore = Math.Max(finalScore, contribVal);
            }

            string cleanArabic = document.PrimaryText ?? string.Empty;

            // 2. Exact Arabic Match Check
            if (!string.IsNullOrWhiteSpace(context.Analysis.Query.Normalized) && 
                cleanArabic.Contains(context.Analysis.Query.Normalized, StringComparison.OrdinalIgnoreCase))
            {
                double weight = _weightsProvider.GetWeight("Arabic");
                double contribVal = weight * 1.0;
                contributions.Add(new RankingContribution(
                    Factor: RankingFactor.ExactWord,
                    Weight: weight,
                    Value: 1.0,
                    Contribution: contribVal
                ));
                finalScore = Math.Max(finalScore, contribVal);
            }

            // 3. Exact Translation Match Check
            foreach (var translation in document.Translations)
            {
                if (!string.IsNullOrWhiteSpace(context.Analysis.Query.Normalized) && 
                    translation.Text.Contains(context.Analysis.Query.Normalized, StringComparison.OrdinalIgnoreCase))
                {
                    double weight = _weightsProvider.GetWeight("Translation");
                    double contribVal = weight * 1.0;
                    contributions.Add(new RankingContribution(
                        Factor: RankingFactor.TranslationPriority,
                        Weight: weight,
                        Value: 1.0,
                        Contribution: contribVal
                    ));
                    finalScore = Math.Max(finalScore, contribVal);
                }
            }

            // 4. Token & Synonym level matches
            foreach (var token in context.Analysis.Query.Tokens)
            {
                if (cleanArabic.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    double weight = _weightsProvider.GetWeight("Partial");
                    double contribVal = weight * 1.0;
                    contributions.Add(new RankingContribution(
                        Factor: RankingFactor.Prefix,
                        Weight: weight,
                        Value: 1.0,
                        Contribution: contribVal
                    ));
                    finalScore = Math.Max(finalScore, contribVal);
                }
            }

            // 5. Synonym expansion matches
            foreach (var synToken in context.Analysis.Query.Synonyms)
            {
                foreach (var trans in document.Translations)
                {
                    if (trans.Text.Contains(synToken, StringComparison.OrdinalIgnoreCase))
                    {
                        double weight = _weightsProvider.GetWeight("Synonym");
                        double contribVal = weight * 0.8;
                        contributions.Add(new RankingContribution(
                            Factor: RankingFactor.Synonym,
                            Weight: weight,
                            Value: 0.8,
                            Contribution: contribVal
                        ));
                        finalScore = Math.Max(finalScore, contribVal);
                    }
                }
            }

            // 6. Semantic Similarity Match Check
            if (item.Method == RetrievalMethod.Semantic || item.Method == RetrievalMethod.Hybrid)
            {
                double weight = _weightsProvider.GetWeight("Semantic");
                if (weight == 0) weight = 35.0; // Default fallback weight

                double contribVal = weight * item.RetrievalScore;
                contributions.Add(new RankingContribution(
                    Factor: RankingFactor.SemanticSimilarity,
                    Weight: weight,
                    Value: item.RetrievalScore,
                    Contribution: contribVal
                ));
                finalScore = Math.Max(finalScore, contribVal);
            }

            // 7. Source Priorities (Primary boost)
            if (document.Source == EvidenceSource.Quran)
            {
                double weight = 2.0; // Priority weight
                contributions.Add(new RankingContribution(
                    Factor: RankingFactor.CollectionPriority,
                    Weight: weight,
                    Value: 1.0,
                    Contribution: weight * 1.0
                ));
                finalScore = Math.Min(100.0, finalScore + (weight * 1.0));
            }

            var rankingScore = new RankingScore(
                FinalValue: finalScore,
                Contributions: contributions
            );

            // Construct explainable RetrievalEvidence
            var matchedConcepts = context.Analysis.SemanticQuery?.Concepts ?? new List<string>();
            var matchedRoots = context.Analysis.SemanticQuery?.ArabicRoots ?? new List<string>();
            var expandedTerms = context.Analysis.SemanticQuery?.ExpandedTokens ?? new List<string>();

            var semanticEvidence = new SemanticEvidence(
                SemanticSimilarity: item.Method == RetrievalMethod.Lexical ? 0.0 : item.RetrievalScore,
                MatchedConcepts: matchedConcepts,
                MatchedRoots: matchedRoots,
                ExpandedTerms: expandedTerms
            );

            var evidence = new RetrievalEvidence(
                Method: item.Method,
                Similarity: item.RetrievalScore,
                Semantic: semanticEvidence,
                Explanation: $"Retrieved via {item.Method} search. Similarity: {item.RetrievalScore:F3}"
            );

            var scoredMatch = new KnowledgeMatch(
                Document: document,
                MatchedTokens: context.Analysis.Query.Tokens,
                Ranking: rankingScore,
                Evidence: evidence
            );

            rankedList.Add(scoredMatch);
        }

        // Sort by final score descending
        var sorted = rankedList
            .OrderByDescending(c => c.Ranking.FinalValue)
            .ToList();

        return context with
        {
            RankedCandidates = sorted
        };
    }

    private static bool IsReferenceMatch(EvidenceSource source, ResearchReference candidateRef, ResearchReference targetRef)
    {
        if (source != targetRef.Source) return false;

        if (candidateRef is QuranReference qcand && targetRef is QuranReference qtarg)
        {
            return qcand.Surah == qtarg.Surah && qcand.Ayah == qtarg.Ayah;
        }

        if (candidateRef is HadithReference hcand && targetRef is HadithReference htarg)
        {
            return string.Equals(hcand.Collection, htarg.Collection, StringComparison.OrdinalIgnoreCase) && 
                   hcand.HadithNumber == htarg.HadithNumber;
        }

        return false;
    }
}
