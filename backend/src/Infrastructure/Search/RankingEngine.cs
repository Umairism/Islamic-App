using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

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
        if (context == null || context.CandidatesList.Count == 0)
            return context ?? null!;

        var rankedList = new List<KnowledgeMatch>();

        foreach (var candidate in context.CandidatesList)
        {
            var contributions = new List<RankingContribution>();
            double finalScore = 0;

            // 1. Exact Reference Match Check
            if (context.Analysis.ParsedReference != null && 
                IsReferenceMatch(candidate.Document.Source, candidate.Document.Reference, context.Analysis.ParsedReference))
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

            string cleanArabic = candidate.Document.PrimaryText ?? string.Empty;

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
            foreach (var translation in candidate.Document.Translations)
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
                foreach (var trans in candidate.Document.Translations)
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

            // 6. Source Priorities (Primary boost)
            if (candidate.Document.Source == EvidenceSource.Quran)
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

            var scoredMatch = candidate with { Ranking = rankingScore };
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
