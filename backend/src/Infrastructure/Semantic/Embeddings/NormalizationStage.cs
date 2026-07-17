using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Semantic.Embeddings;

public class NormalizationStage : IEmbeddingPipelineStage
{
    private readonly ISearchNormalizer _normalizer;

    public NormalizationStage(ISearchNormalizer normalizer)
    {
        _normalizer = normalizer;
    }

    public Task<EmbeddingPipelineContext> ExecuteAsync(
        EmbeddingPipelineContext context,
        CancellationToken cancellationToken)
    {
        string normalized = _normalizer.Normalize(context.Request.Text);
        
        var updated = context with
        {
            NormalizedText = normalized,
            ProcessingHistory = context.ProcessingHistory.Add("NormalizationStage")
        };
        
        return Task.FromResult(updated);
    }
}
