namespace IslamicApp.Application.Retrieval.Semantic;

public interface ISimilarityMetric
{
    double Calculate(float[] v1, float[] v2);
}
