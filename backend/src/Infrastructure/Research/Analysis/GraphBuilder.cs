using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Infrastructure.Research.Analysis;

public class GraphBuilder : IGraphBuilder
{
    private readonly IEvidenceAnalyzer _analyzer;

    public GraphBuilder(IEvidenceAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public EvidenceGraph BuildGraph(EvidenceCorpus corpus, QueryAnalysis analysis)
    {
        var nodes = new List<EvidenceNode>();
        var relationships = new List<EvidenceRelationship>();
        
        // Map of DocumentId to NodeId
        var docToNodeMap = new Dictionary<DocumentId, NodeId>();
        var nodeMap = new Dictionary<NodeId, ResearchEvidence>();

        // 1. Build Nodes
        foreach (var evidence in corpus.Evidences)
        {
            var nodeId = GenerateNodeId(evidence.Reference.Value, evidence.Source);
            var classification = _analyzer.Classify(evidence);
            
            // Derive confidence from retrieval score normalized to 0.0 - 1.0 range
            double normalizedScore = Math.Clamp(evidence.RetrievalScore / 100.0, 0.0, 1.0);
            var confidence = new ConfidenceScore(normalizedScore);

            var node = new EvidenceNode(nodeId, evidence.Id, classification, confidence);
            nodes.Add(node);
            
            docToNodeMap[evidence.Id] = nodeId;
            nodeMap[nodeId] = evidence;
        }

        // 2. Build Relationships
        var nodeIds = nodeMap.Keys.ToList();
        for (int i = 0; i < nodeIds.Count; i++)
        {
            var sourceId = nodeIds[i];
            var sourceEv = nodeMap[sourceId];

            for (int j = i + 1; j < nodeIds.Count; j++)
            {
                var targetId = nodeIds[j];
                var targetEv = nodeMap[targetId];

                // Check cross-reference mentions (e.g. text of one references the other)
                if (sourceEv.Content.Contains(targetEv.Reference.Value) || targetEv.Content.Contains(sourceEv.Reference.Value))
                {
                    relationships.Add(new EvidenceRelationship(
                        SourceNodeId: sourceId,
                        TargetNodeId: targetId,
                        Type: EvidenceRelationshipType.Explains,
                        Description: $"Text cross-reference match between {sourceEv.Reference.Value} and {targetEv.Reference.Value}"
                    ));
                }

                // Check Surah/Ayah explanation (e.g. Hadith references Quran verse)
                if (sourceEv.Source == EvidenceSource.Hadith && targetEv.Source == EvidenceSource.Quran)
                {
                    // Check if Hadith text mentions Quran Surah number
                    var surahPart = targetEv.Reference.Value.Split(':')[0];
                    if (sourceEv.Content.Contains($"surah {surahPart}") || sourceEv.Content.Contains($"chapter {surahPart}"))
                    {
                        relationships.Add(new EvidenceRelationship(
                            SourceNodeId: sourceId,
                            TargetNodeId: targetId,
                            Type: EvidenceRelationshipType.Supports,
                            Description: $"Hadith supports Quran surah context of {targetEv.Reference.Value}"
                        ));
                    }
                }

                // Check topic overlaps
                var overlappingTopics = sourceEv.Topics.Intersect(targetEv.Topics).ToList();
                if (overlappingTopics.Count > 0)
                {
                    var topicsList = string.Join(", ", overlappingTopics.Select(t => t.Value));
                    relationships.Add(new EvidenceRelationship(
                        SourceNodeId: sourceId,
                        TargetNodeId: targetId,
                        Type: EvidenceRelationshipType.Clarifies,
                        Description: $"Shared topics: [{topicsList}] clarifies conceptual connection"
                    ));
                }
            }
        }

        return new EvidenceGraph(nodes, relationships);
    }

    private NodeId GenerateNodeId(string reference, EvidenceSource source)
    {
        var input = $"{source}:{reference}".ToLowerInvariant();
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        var base64 = Convert.ToBase64String(hash)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "")
            .Substring(0, 16);
        return new NodeId(base64);
    }
}
