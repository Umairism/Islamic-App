using System.Threading;
using System.Threading.Tasks;

namespace IslamicApp.Application.Retrieval.Benchmark;

public record BenchmarkResult(
    double PrecisionAt10,
    double RecallAt10,
    double Mrr,
    double Ndcg,
    double AverageLatencyMs,
    double P95LatencyMs,
    double EmbeddingTimeMs,
    double RetrievalTimeMs,
    double FusionTimeMs
);

public interface ISemanticBenchmarkRunner
{
    Task<BenchmarkResult> RunEvaluationsAsync(CancellationToken cancellationToken);
}
