using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Semantic.Vector;

public class DotProductSimilarity : ISimilarityMetric
{
    public double Calculate(float[] v1, float[] v2)
    {
        if (v1 == null || v2 == null || v1.Length != v2.Length) return 0;

        double dotProduct = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            dotProduct += v1[i] * v2[i];
        }
        return dotProduct;
    }
}
