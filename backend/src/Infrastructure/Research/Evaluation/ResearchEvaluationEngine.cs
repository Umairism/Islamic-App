using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using IslamicApp.Application.Research.Evaluation;
using IslamicApp.Application.Research.Evaluation.Models;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Evaluation;

public class ResearchEvaluationEngine : IResearchEvaluator
{
    private readonly ICitationVerifier _citationVerifier;
    private readonly EvaluationOptions _options;

    public ResearchEvaluationEngine(
        ICitationVerifier citationVerifier,
        IOptions<EvaluationOptions> options)
    {
        _citationVerifier = citationVerifier;
        _options = options?.Value ?? new EvaluationOptions();
    }

    public async Task<EvaluationResult> EvaluateAsync(
        ResearchExecutionContext executionContext,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<EvaluationFinding>();
        var claims = executionContext.Reasoning?.Claims ?? new List<ResearchClaim>();
        var corpus = executionContext.Context?.Input?.Corpus;

        // 1. Calculate Evidence Coverage & Verify Citations
        int totalCitations = 0;
        int verifiedCitations = 0;
        double sumRelevance = 0;

        foreach (var claim in claims)
        {
            if (claim.SupportingEvidence == null || claim.SupportingEvidence.Count == 0)
            {
                findings.Add(new EvaluationFinding(
                    Category: "Coverage",
                    Description: $"Claim '{claim.Statement}' lacks supporting evidence citations.",
                    Severity: ErrorSeverity.Warning
                ));
                continue;
            }

            foreach (var evRef in claim.SupportingEvidence)
            {
                totalCitations++;
                var verifyResult = await _citationVerifier.VerifyAsync(
                    evRef,
                    claim.Statement,
                    corpus,
                    cancellationToken
                );

                if (verifyResult.Exists)
                {
                    verifiedCitations++;
                    sumRelevance += verifyResult.RelevanceScore;
                }
                else
                {
                    findings.Add(new EvaluationFinding(
                        Category: "CitationIntegrity",
                        Description: $"Claim cites reference '{evRef.Value}' which is missing from corpus.",
                        Severity: ErrorSeverity.Error
                    ));
                }
            }

            // Detect overreaching claims ("unanimous", "all scholars") with low evidence count
            var stmtLower = claim.Statement.ToLowerInvariant();
            if ((stmtLower.Contains("unanimous") || stmtLower.Contains("all scholars") || stmtLower.Contains("every scholar")) &&
                claim.SupportingEvidence.Count < 3)
            {
                findings.Add(new EvaluationFinding(
                    Category: "ClaimOverreach",
                    Description: $"Claim '{claim.Statement}' asserts broad consensus but cites only {claim.SupportingEvidence.Count} evidence sources.",
                    Severity: ErrorSeverity.Warning
                ));
            }
        }

        double evidenceCoverage = claims.Count > 0
            ? (double)claims.Count(c => c.SupportingEvidence != null && c.SupportingEvidence.Count > 0) / claims.Count
            : 1.0;

        double citationAccuracy = totalCitations > 0
            ? ((double)verifiedCitations / totalCitations) * (sumRelevance / Math.Max(1, verifiedCitations))
            : 1.0;

        // 2. Reasoning Consistency Score
        double reasoningConsistency = claims.Count > 0
            ? claims.Average(c => c.Confidence.Value)
            : 0.9;

        // 3. Source Diversity Score (Quran vs Hadith vs Tafsir)
        double sourceDiversity = 0.5;
        if (corpus?.Evidences != null && corpus.Evidences.Count > 0)
        {
            var sources = corpus.Evidences.Select(e => e.Source).Distinct().Count();
            sourceDiversity = Math.Min(1.0, 0.4 + (sources * 0.3));
        }

        // 4. Calculate Weighted Overall Score
        var w = _options.Weights;
        double overallScore = (evidenceCoverage * w.EvidenceCoverage) +
                             (citationAccuracy * w.CitationAccuracy) +
                             (reasoningConsistency * w.ReasoningConsistency) +
                             (sourceDiversity * w.SourceDiversity);

        overallScore = Math.Round(Math.Max(0.0, Math.Min(1.0, overallScore)), 2);

        var score = new ResearchQualityScore(
            EvidenceCoverage: Math.Round(evidenceCoverage, 2),
            CitationAccuracy: Math.Round(citationAccuracy, 2),
            ReasoningConsistency: Math.Round(reasoningConsistency, 2),
            SourceDiversity: Math.Round(sourceDiversity, 2),
            OverallScore: overallScore
        );

        var sessionId = executionContext.Session?.SessionId ?? Guid.NewGuid();

        return new EvaluationResult(
            ResearchSessionId: sessionId,
            Score: score,
            Findings: findings,
            EvaluationVersion: _options.Version,
            EvaluatedAt: DateTimeOffset.UtcNow
        );
    }
}
