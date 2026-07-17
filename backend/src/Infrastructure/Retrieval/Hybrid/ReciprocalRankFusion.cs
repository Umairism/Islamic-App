using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Retrieval.Hybrid;

namespace IslamicApp.Infrastructure.Retrieval.Hybrid;

public class ReciprocalRankFusion : IFusionStrategy
{
    private readonly int _k;

    public ReciprocalRankFusion(int k = 60)
    {
        _k = k;
    }

    public List<CandidateDocument> Fuse(
        List<CandidateDocument> lexicalCandidates,
        List<CandidateDocument> semanticCandidates)
    {
        var lexicalMap = lexicalCandidates
            .Select((doc, index) => new { Doc = doc, Rank = index + 1 })
            .ToDictionary(x => x.Doc.Id, x => x.Rank);

        var semanticMap = semanticCandidates
            .Select((doc, index) => new { Doc = doc, Rank = index + 1 })
            .ToDictionary(x => x.Doc.Id, x => x.Rank);

        var allDocs = lexicalCandidates.Concat(semanticCandidates)
            .GroupBy(d => d.Id)
            .Select(g => g.First())
            .ToList();

        var rrfScores = new Dictionary<string, double>();
        foreach (var doc in allDocs)
        {
            double score = 0;
            if (lexicalMap.TryGetValue(doc.Id, out int lexRank))
            {
                score += 1.0 / (_k + lexRank);
            }
            if (semanticMap.TryGetValue(doc.Id, out int semRank))
            {
                score += 1.0 / (_k + semRank);
            }
            rrfScores[doc.Id] = score;
        }

        double maxRrf = rrfScores.Values.DefaultIfEmpty(1.0).Max();

        var fused = allDocs
            .Select(doc => doc with
            {
                Score = (float)(rrfScores[doc.Id] / maxRrf),
                Method = RetrievalMethod.Hybrid
            })
            .OrderByDescending(d => d.Score)
            .ToList();

        return fused;
    }
}
