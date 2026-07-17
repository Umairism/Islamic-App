using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Retrieval.Semantic;

namespace IslamicApp.Infrastructure.Semantic.Embeddings;

public class EmbeddingGenerationStage : IEmbeddingPipelineStage
{
    private readonly IEmbeddingGenerator _generator;
    private readonly IEmbeddingCache _cache;

    public EmbeddingGenerationStage(IEmbeddingGenerator generator, IEmbeddingCache cache)
    {
        _generator = generator;
        _cache = cache;
    }

    public async Task<EmbeddingPipelineContext> ExecuteAsync(
        EmbeddingPipelineContext context,
        CancellationToken cancellationToken)
    {
        string lookupText = context.NormalizedText;
        var language = context.Request.Language;

        if (_cache.TryGet(lookupText, language, out var cachedVector))
        {
            return context with
            {
                Vector = cachedVector,
                FromCache = true,
                ProcessingHistory = context.ProcessingHistory.Add("EmbeddingGenerationStage: CacheHit")
            };
        }

        var generated = await _generator.GenerateAsync(lookupText, language, cancellationToken);
        _cache.Store(lookupText, language, generated);

        return context with
        {
            Vector = generated,
            FromCache = false,
            ProcessingHistory = context.ProcessingHistory.Add("EmbeddingGenerationStage: CacheMiss")
        };
    }
}
