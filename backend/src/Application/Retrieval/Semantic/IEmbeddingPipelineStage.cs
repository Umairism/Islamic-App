using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Retrieval.Semantic;

public record EmbeddingRequest(
    string Text,
    ResearchLanguage Language
);

public record EmbeddingPipelineContext(
    EmbeddingRequest Request,
    string NormalizedText,
    float[]? Vector,
    bool FromCache,
    ImmutableList<string> ProcessingHistory
);

public interface IEmbeddingPipelineStage
{
    Task<EmbeddingPipelineContext> ExecuteAsync(
        EmbeddingPipelineContext context, 
        CancellationToken cancellationToken);
}

public interface IEmbeddingPipeline
{
    Task<EmbeddingPipelineContext> ProcessAsync(
        EmbeddingRequest request, 
        CancellationToken cancellationToken);
}
