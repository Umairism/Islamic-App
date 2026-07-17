using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Semantic.Embeddings;

public class MockEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly int _dimensions;

    public MockEmbeddingGenerator(int dimensions = 384)
    {
        _dimensions = dimensions;
    }

    public Task<float[]> GenerateAsync(
        string text,
        ResearchLanguage language,
        CancellationToken cancellationToken)
    {
        float[] vector = new float[_dimensions];
        if (string.IsNullOrEmpty(text)) return Task.FromResult(vector);

        int hash = text.GetHashCode();
        for (int i = 0; i < _dimensions; i++)
        {
            hash = (hash * 31) + i;
            vector[i] = (float)(hash % 1000) / 1000f;
        }

        double sum = 0;
        for (int i = 0; i < _dimensions; i++) sum += vector[i] * vector[i];
        double magnitude = System.Math.Sqrt(sum);
        if (magnitude > 0)
        {
            for (int i = 0; i < _dimensions; i++) vector[i] = (float)(vector[i] / magnitude);
        }

        return Task.FromResult(vector);
    }
}
