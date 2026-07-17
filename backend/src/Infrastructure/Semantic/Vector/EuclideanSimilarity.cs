using System;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Semantic.Vector;

public class EuclideanSimilarity : ISimilarityMetric
{
    public double Calculate(float[] v1, float[] v2)
    {
        if (v1 == null || v2 == null || v1.Length != v2.Length) return 0;

        double distanceSquared = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            double diff = v1[i] - v2[i];
            distanceSquared += diff * diff;
        }

        double distance = Math.Sqrt(distanceSquared);
        return 1.0 / (1.0 + distance);
    }
}
