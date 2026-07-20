using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Evaluation;
using IslamicApp.Application.Research.Evaluation.Models;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Evaluation;

public class CitationVerificationService : ICitationVerifier
{
    public Task<CitationVerificationResult> VerifyAsync(
        ReferenceId referenceId,
        string claimText,
        EvidenceCorpus corpus,
        CancellationToken cancellationToken = default)
    {
        if (corpus == null || corpus.Evidences == null || corpus.Evidences.Count == 0)
        {
            return Task.FromResult(new CitationVerificationResult(
                Exists: false,
                RelevanceScore: 0.0,
                Explanation: $"Citation reference '{referenceId.Value}' could not be verified because the evidence corpus is empty."
            ));
        }

        // Level 1: Integrity Check (Does reference exist in corpus?)
        var evidence = corpus.Evidences.FirstOrDefault(e =>
            e.Reference.Value.Equals(referenceId.Value, StringComparison.OrdinalIgnoreCase) ||
            e.Id.Value.Equals(referenceId.Value, StringComparison.OrdinalIgnoreCase));

        if (evidence == null)
        {
            return Task.FromResult(new CitationVerificationResult(
                Exists: false,
                RelevanceScore: 0.0,
                Explanation: $"Citation reference '{referenceId.Value}' was not found in the retrieved evidence corpus."
            ));
        }

        // Level 2: Relevance Check (Does evidence content support claim?)
        var claimTokens = claimText.Split(new[] { ' ', ',', '.', ';', ':', '(', ')', '"' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 3)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .ToList();

        if (claimTokens.Count == 0)
        {
            return Task.FromResult(new CitationVerificationResult(
                Exists: true,
                RelevanceScore: 1.0,
                Explanation: $"Citation reference '{referenceId.Value}' exists in corpus."
            ));
        }

        var contentLower = (evidence.Content + " " + evidence.Title).ToLowerInvariant();
        int matchingTokens = claimTokens.Count(t => contentLower.Contains(t));
        double relevanceScore = Math.Min(1.0, (double)matchingTokens / Math.Max(1, claimTokens.Count / 2));
        relevanceScore = Math.Max(0.5, relevanceScore); // Baseline relevance for valid reference match

        string explanation = relevanceScore >= 0.8
            ? $"Citation '{referenceId.Value}' strongly supports claim."
            : $"Citation '{referenceId.Value}' exists but offers moderate semantic alignment.";

        return Task.FromResult(new CitationVerificationResult(
            Exists: true,
            RelevanceScore: relevanceScore,
            Explanation: explanation
        ));
    }
}
