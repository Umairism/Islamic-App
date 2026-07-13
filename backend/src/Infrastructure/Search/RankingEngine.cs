using System;
using System.Collections.Generic;
using System.Linq;
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

    public void Rank(SearchContext context)
    {
        if (context == null || context.Candidates.Count == 0)
            return;

        foreach (var candidate in context.Candidates)
        {
            double maxScore = 0;
            var reasons = new List<string>();
            var matchedTerms = new List<string>();

            // 1. Reference Match Check
            if (context.ResolvedReference != null && 
                IsReferenceMatch(candidate.Reference, context.ResolvedReference.Reference))
            {
                // Check if the resolved reference was an alias or standard reference match
                bool isAlias = context.UniqueTokens.Any(t => 
                    string.Equals(t, "ayat", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(t, "kursi", StringComparison.OrdinalIgnoreCase));
                
                double refScore = isAlias ? _config.Alias : _config.Reference;
                if (refScore > maxScore)
                {
                    maxScore = refScore;
                    reasons.Add(isAlias ? "Alias reference match" : "Exact reference match");
                }
            }

            // Normalize Candidate Primary text (Arabic) for accurate match check
            string cleanArabic = candidate.PrimaryText ?? string.Empty;

            // 2. Exact Arabic Match
            if (!string.IsNullOrWhiteSpace(context.NormalizedQuery) && 
                cleanArabic.Contains(context.NormalizedQuery, StringComparison.OrdinalIgnoreCase))
            {
                if (_config.Arabic > maxScore)
                {
                    maxScore = _config.Arabic;
                    reasons.Add("Exact Arabic phrase match");
                }
                matchedTerms.Add(context.NormalizedQuery);
            }

            // 3. Exact Translation Match
            foreach (var translation in candidate.Translations)
            {
                if (!string.IsNullOrWhiteSpace(context.NormalizedQuery) && 
                    translation.Text.Contains(context.NormalizedQuery, StringComparison.OrdinalIgnoreCase))
                {
                    if (_config.Translation > maxScore)
                    {
                        maxScore = _config.Translation;
                        reasons.Add($"Exact translation match in {translation.Language}");
                    }
                    matchedTerms.Add(context.NormalizedQuery);
                }
            }

            // 4. Token level match & synonyms
            foreach (var token in context.UniqueTokens)
            {
                // Arabic token match
                if (cleanArabic.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    double score = _config.Partial;
                    if (score > maxScore)
                    {
                        maxScore = score;
                        reasons.Add($"Partial Arabic match for term: {token}");
                    }
                    matchedTerms.Add(token);
                }

                // Translation token match
                foreach (var trans in candidate.Translations)
                {
                    if (trans.Text.Contains(token, StringComparison.OrdinalIgnoreCase))
                    {
                        double score = _config.Partial;
                        if (score > maxScore)
                        {
                            maxScore = score;
                            reasons.Add($"Partial translation match for term: {token}");
                        }
                        matchedTerms.Add(token);
                    }
                }
            }

            // 5. Synonym matches
            foreach (var expandedToken in context.ExpandedTokens)
            {
                // Skip if already matched as an original query token
                if (context.UniqueTokens.Contains(expandedToken, StringComparer.OrdinalIgnoreCase))
                    continue;

                foreach (var trans in candidate.Translations)
                {
                    if (trans.Text.Contains(expandedToken, StringComparison.OrdinalIgnoreCase))
                    {
                        double score = _config.Synonym;
                        if (score > maxScore)
                        {
                            maxScore = score;
                            reasons.Add($"Synonym match for expanded term: {expandedToken}");
                        }
                        matchedTerms.Add(expandedToken);
                    }
                }
            }

            // 6. Surah Name match
            if (candidate.Metadata.TryGetValue("SurahEnglishName", out var englishNameObj) && englishNameObj is string englishName)
            {
                if (context.UniqueTokens.Any(token => englishName.Contains(token, StringComparison.OrdinalIgnoreCase)))
                {
                    if (_config.SurahName > maxScore)
                    {
                        maxScore = _config.SurahName;
                        reasons.Add($"Surah name match: {englishName}");
                    }
                }
            }

            candidate.Score = maxScore;
            candidate.Reasons.AddRange(reasons.Distinct());
            candidate.MatchedTerms.AddRange(matchedTerms.Distinct());
        }

        // Sort by score descending, then by reference ascending
        context.RankedCandidates = context.Candidates
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Reference)
            .ToList();
    }

    private static bool IsReferenceMatch(string candidateRef, string targetRef)
    {
        if (string.Equals(candidateRef, targetRef, StringComparison.OrdinalIgnoreCase))
            return true;

        // Resolve range references (e.g. target "2:285-286" matches candidate "2:285" or "2:286")
        if (targetRef.Contains("-"))
        {
            var parts = targetRef.Split(':');
            if (parts.Length == 2)
            {
                string surah = parts[0];
                var rangeParts = parts[1].Split('-');
                if (rangeParts.Length == 2 && int.TryParse(rangeParts[0], out int start) && int.TryParse(rangeParts[1], out int end))
                {
                    var candParts = candidateRef.Split(':');
                    if (candParts.Length == 2 && candParts[0] == surah && int.TryParse(candParts[1], out int candAyah))
                    {
                        return candAyah >= start && candAyah <= end;
                    }
                }
            }
        }

        return false;
    }
}
