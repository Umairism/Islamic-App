using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Infrastructure.Research;

public class ExplainabilityBuilder : IExplainabilityBuilder
{
    public ExplainabilityMap BuildMap(ReasoningResult reasoning, ResearchContext context)
    {
        var traces = new List<SourceTraceLink>();

        var textToTrace = $"{reasoning.Summary}";
        foreach (var finding in reasoning.Findings)
        {
            textToTrace += $" {finding.Details}";
        }

        var sentences = Regex.Split(textToTrace, @"(?<=[\.\?!])\s+")
            .Select(s => s.Trim())
            .Where(s => s.Length > 10)
            .Distinct()
            .ToList();

        var referenceToNodeMap = new Dictionary<string, NodeId>();
        if (context.Input.Corpus != null)
        {
            foreach (var e in context.Input.Corpus.Evidences)
            {
                var nodeIdVal = ComputeNodeId(e.Source, e.Reference.Value);
                referenceToNodeMap[e.Reference.Value.ToLowerInvariant()] = new NodeId(nodeIdVal);
            }
        }

        foreach (var sentence in sentences)
        {
            var matchedNodeIds = new List<NodeId>();
            var sentenceLower = sentence.ToLowerInvariant();
            var correspondingClaims = reasoning.Claims
                .Where(c => ClaimOverlapsWithSentence(c, sentenceLower))
                .ToList();

            foreach (var claim in correspondingClaims)
            {
                foreach (var evidenceRef in claim.SupportingEvidence)
                {
                    if (referenceToNodeMap.TryGetValue(evidenceRef.Value.ToLowerInvariant(), out var nodeId))
                    {
                        if (!matchedNodeIds.Contains(nodeId))
                        {
                            matchedNodeIds.Add(nodeId);
                        }
                    }
                }
            }

            if (matchedNodeIds.Count == 0 && context.Input.Corpus != null)
            {
                foreach (var evidence in context.Input.Corpus.Evidences)
                {
                    if (sentenceLower.Contains(evidence.Reference.Value.ToLowerInvariant()))
                    {
                        var nodeIdVal = ComputeNodeId(evidence.Source, evidence.Reference.Value);
                        matchedNodeIds.Add(new NodeId(nodeIdVal));
                    }
                }
            }

            if (matchedNodeIds.Count > 0)
            {
                var confidenceScore = matchedNodeIds.Count > 1 ? new ConfidenceScore(0.85) : new ConfidenceScore(0.95);
                traces.Add(new SourceTraceLink(
                    Sentence: sentence,
                    EvidencePath: matchedNodeIds,
                    TraceConfidence: confidenceScore
                ));
            }
        }

        return new ExplainabilityMap(traces);
    }

    private bool ClaimOverlapsWithSentence(ResearchClaim claim, string sentenceLower)
    {
        var statementLower = claim.Statement.ToLowerInvariant();
        var words = statementLower.Split(new[] { ' ', ',', '.', ';', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4)
            .ToList();

        if (words.Count == 0) return false;
        
        int matches = words.Count(w => sentenceLower.Contains(w));
        return matches >= Math.Min(3, words.Count);
    }

    private string ComputeNodeId(EvidenceSource source, string reference)
    {
        var rawKey = $"{source.ToString().ToLowerInvariant()}:{reference.ToLowerInvariant()}";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToBase64String(bytes)[..16];
    }
}
