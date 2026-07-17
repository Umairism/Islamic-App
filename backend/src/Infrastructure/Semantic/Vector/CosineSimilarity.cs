using System;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Semantic.Vector;

public class CosineSimilarity : ISimilarityMetric
{
    public double Calculate(float[] v1, float[] v2)
    {
        if (v1 == null || v2 == null || v1.Length != v2.Length) return 0;

        double dotProduct = 0;
        double magnitude1 = 0;
        double magnitude2 = 0;

        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
            magnitude1 += v1[i] * v1[i];
            magnitude2 += v2[i] * v2[i];
        }

        if (magnitude1 == 0 || magnitude2 == 0) return 0;

        return dotProduct / (Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));
    }
}
