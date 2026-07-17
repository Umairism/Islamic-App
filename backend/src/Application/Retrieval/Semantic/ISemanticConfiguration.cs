using IslamicApp.Application.Retrieval.Policies;

namespace IslamicApp.Application.Retrieval.Semantic;

public record SemanticFeatures(
    bool EnableEmbeddings,
    bool EnableHybrid,
    bool EnableCrossReferences,
    bool EnableReasoning,
    bool EnableBenchmarking
);

public interface ISemanticConfiguration
{
    SemanticFeatures Features { get; }
    string EmbeddingProvider { get; }
    string SimilarityMetric { get; }
    string FusionStrategy { get; }
    SemanticPolicy DefaultPolicy { get; }
}
