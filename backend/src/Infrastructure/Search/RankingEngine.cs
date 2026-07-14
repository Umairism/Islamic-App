using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search;

public class RankingEngine : IRankingEngine
{
    private readonly IRankingConfiguration _config;

    public RankingEngine(IRankingConfiguration config)
    {
        _config = config;
    }

    public SearchContext Rank(SearchContext context)
    {
        if (context == null || context.CandidatesList.Count == 0)
            return context ?? null!;

        var rankedList = new List<EvidenceMatch>();

        foreach (var candidate in context.CandidatesList)
        {
            double maxScore = 0;
            var reasons = new List<string>();
            var matchedTerms = new List<string>();
            
            double textMatchScore = 0;
            double refMatchScore = 0;
            
            var boosts = new List<string>();
            var penalties = new List<string>();
            var strategies = new List<string>();

            // 1. Reference Match Check
            if (context.ResolvedReference != null && 
                IsReferenceMatch(candidate.Source, candidate.Reference, context.ResolvedReference.Identifier))
            {
                bool isAlias = context.UniqueTokensList.Any(t => 
                    string.Equals(t, "ayat", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(t, "kursi", StringComparison.OrdinalIgnoreCase));
                
                double refScore = isAlias ? _config.Alias : _config.Reference;
                refMatchScore = isAlias ? 95 : 100;
                
                if (refScore > maxScore)
                {
                    maxScore = refScore;
                    reasons.Add(isAlias ? "Alias reference match" : "Exact reference match");
                    strategies.Add(isAlias ? "AliasMatch" : "ExactReferenceMatch");
                }
            }

            // Clean Arabic text
            string cleanArabic = candidate.PrimaryText ?? string.Empty;

            // 2. Exact Arabic Match
            if (!string.IsNullOrWhiteSpace(context.NormalizedQuery) && 
                cleanArabic.Contains(context.NormalizedQuery, StringComparison.OrdinalIgnoreCase))
            {
                textMatchScore = 100;
                if (_config.Arabic > maxScore)
                {
                    maxScore = _config.Arabic;
                    reasons.Add("Exact Arabic phrase match");
                    strategies.Add("ExactArabicMatch");
                }
                matchedTerms.Add(context.NormalizedQuery);
            }

            // 3. Exact Translation Match
            foreach (var translation in candidate.Translations)
            {
                if (!string.IsNullOrWhiteSpace(context.NormalizedQuery) && 
                    translation.Text.Contains(context.NormalizedQuery, StringComparison.OrdinalIgnoreCase))
                {
                    textMatchScore = 100;
                    if (_config.Translation > maxScore)
                    {
                        maxScore = _config.Translation;
                        reasons.Add($"Exact translation match in {translation.Language}");
                        strategies.Add("ExactTranslationMatch");
                    }
                    matchedTerms.Add(context.NormalizedQuery);
                }
            }

            // 4. Token level match & synonyms
            foreach (var token in context.UniqueTokensList)
            {
                // Arabic token match
                if (cleanArabic.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    textMatchScore = Math.Max(textMatchScore, 40);
                    double score = _config.Partial;
                    if (score > maxScore)
                    {
                        maxScore = score;
                        reasons.Add($"Partial Arabic match for term: {token}");
                        strategies.Add("PartialArabicMatch");
                    }
                    matchedTerms.Add(token);
                }

                // Translation token match
                foreach (var trans in candidate.Translations)
                {
                    if (trans.Text.Contains(token, StringComparison.OrdinalIgnoreCase))
                    {
                        textMatchScore = Math.Max(textMatchScore, 40);
                        double score = _config.Partial;
                        if (score > maxScore)
                        {
                            maxScore = score;
                            reasons.Add($"Partial translation match for term: {token}");
                            strategies.Add("PartialTranslationMatch");
                        }
                        matchedTerms.Add(token);
                    }
                }
            }

            // 5. Synonym matches
            foreach (var expandedToken in context.ExpandedTokensList)
            {
                if (context.UniqueTokensList.Contains(expandedToken, StringComparer.OrdinalIgnoreCase))
                    continue;

                foreach (var trans in candidate.Translations)
                {
                    if (trans.Text.Contains(expandedToken, StringComparison.OrdinalIgnoreCase))
                    {
                        textMatchScore = Math.Max(textMatchScore, 30);
                        double score = _config.Synonym;
                        if (score > maxScore)
                        {
                            maxScore = score;
                            reasons.Add($"Synonym match for expanded term: {expandedToken}");
                            strategies.Add("SynonymMatch");
                        }
                        matchedTerms.Add(expandedToken);
                    }
                }
            }

            // 6. Metadata matching factors (e.g. source priority)
            if (candidate.Source == EvidenceSource.Quran)
            {
                // Assign a subtle boost for primary source
                maxScore = Math.Min(100.0, maxScore + 2.0);
                boosts.Add("Quran Divine Revelation Source Priority (+2.0)");
            }

            candidate.Score = maxScore;
            candidate.Reasons.AddRange(reasons.Distinct());
            candidate.MatchedTerms.AddRange(matchedTerms.Distinct());

            // Build Structured Explanation
            candidate.Explanation = new SearchExplanation(
                TokenMatches: matchedTerms.Distinct().ToList(),
                ReferenceMatches: context.ResolvedReference != null ? new List<string> { context.ResolvedReference.FormattedReference } : new List<string>(),
                Boosts: boosts,
                Penalties: penalties,
                RankingFactors: new Dictionary<string, double>
                {
                    { "TextMatchWeight", textMatchScore },
                    { "ReferenceMatchWeight", refMatchScore },
                    { "BaseScore", maxScore }
                }
            );

            // Compute Structured EvidenceConfidence
            string authority = candidate.Source == EvidenceSource.Quran ? "Primary (Divine Revelation)" : "Secondary (Authentic Narration)";
            
            double overallConfidence = 0;
            if (refMatchScore == 100) overallConfidence = candidate.Source == EvidenceSource.Quran ? 98.0 : 92.0;
            else if (refMatchScore == 95) overallConfidence = candidate.Source == EvidenceSource.Quran ? 95.0 : 88.0;
            else if (textMatchScore == 100) overallConfidence = candidate.Source == EvidenceSource.Quran ? 88.0 : 82.0;
            else overallConfidence = Math.Clamp(maxScore * 0.9, 0, 100);

            candidate.Confidence = new EvidenceConfidence(
                SourceAuthority: authority,
                TextMatch: textMatchScore,
                ReferenceMatch: refMatchScore,
                RankingScore: maxScore,
                OverallConfidence: overallConfidence
            );

            rankedList.Add(candidate);
        }

        // Sort by score descending, then by reference ascending
        var sorted = rankedList
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Reference)
            .ToList();

        return context with
        {
            RankedCandidates = sorted
        };
    }

    private static bool IsReferenceMatch(EvidenceSource source, string candidateRef, KnowledgeIdentifier target)
    {
        if (source != target.Source) return false;

        if (string.Equals(candidateRef, target.VerseOrHadithNumber, StringComparison.OrdinalIgnoreCase))
            return true;

        // Resolve range references (e.g. target "285-286" matches candidate "285" or "286")
        if (target.VerseOrHadithNumber != null && target.VerseOrHadithNumber.Contains("-"))
        {
            var parts = target.VerseOrHadithNumber.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
            {
                var candParts = candidateRef.Split(':');
                string candAyahStr = candParts.Length == 2 ? candParts[1] : candidateRef;
                if (int.TryParse(candAyahStr, out int candVal))
                {
                    return candVal >= start && candVal <= end;
                }
            }
        }

        // Support exact Qur'an reference format matching (e.g., candidate "2:255" matches target book="2" verse="255")
        if (source == EvidenceSource.Quran && target.Book != null)
        {
            string expected = $"{target.Book}:{target.VerseOrHadithNumber}";
            return string.Equals(candidateRef, expected, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
