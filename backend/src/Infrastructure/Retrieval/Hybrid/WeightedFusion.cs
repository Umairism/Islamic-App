using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Retrieval.Hybrid;

namespace IslamicApp.Infrastructure.Retrieval.Hybrid;

public class WeightedFusion : IFusionStrategy
{
    private readonly double _lexicalWeight;
    private readonly double _semanticWeight;

    public WeightedFusion(double lexicalWeight = 0.5, double semanticWeight = 0.5)
    {
        _lexicalWeight = lexicalWeight;
        _semanticWeight = semanticWeight;
    }

    public List<CandidateDocument> Fuse(
        List<CandidateDocument> lexicalCandidates,
        List<CandidateDocument> semanticCandidates)
    {
        var lexicalScores = lexicalCandidates.ToDictionary(d => d.Id, d => d.Score);
        var semanticScores = semanticCandidates.ToDictionary(d => d.Id, d => d.Score);

        var allDocs = lexicalCandidates.Concat(semanticCandidates)
            .GroupBy(d => d.Id)
            .Select(g => g.First())
            .ToList();

        var fused = new List<CandidateDocument>();
        foreach (var doc in allDocs)
        {
            double lex = lexicalScores.TryGetValue(doc.Id, out float l) ? l : 0.0;
            double sem = semanticScores.TryGetValue(doc.Id, out float s) ? s : 0.0;

            double score = (_lexicalWeight * lex) + (_semanticWeight * sem);

            fused.Add(doc with
            {
                Score = (float)score,
                Method = RetrievalMethod.Hybrid
            });
        }

        return fused.OrderByDescending(d => d.Score).ToList();
    }
}
