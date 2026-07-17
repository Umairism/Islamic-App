using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Semantic.Embeddings;

public class EmbeddingPipeline : IEmbeddingPipeline
{
    private readonly IEnumerable<IEmbeddingPipelineStage> _stages;

    public EmbeddingPipeline(IEnumerable<IEmbeddingPipelineStage> stages)
    {
        _stages = stages;
    }

    public async Task<EmbeddingPipelineContext> ProcessAsync(
        EmbeddingRequest request,
        CancellationToken cancellationToken)
    {
        var context = new EmbeddingPipelineContext(
            Request: request,
            NormalizedText: request.Text,
            Vector: null,
            FromCache: false,
            ProcessingHistory: ImmutableList<string>.Empty
        );

        foreach (var stage in _stages)
        {
            context = await stage.ExecuteAsync(context, cancellationToken);
        }

        return context;
    }
}
