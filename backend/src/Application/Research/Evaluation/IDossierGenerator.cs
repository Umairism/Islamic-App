using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Evaluation.Models;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Evaluation;

public record DossierGenerationResult(
    string MarkdownContent,
    string ContentHash,
    string StoragePath
);

public interface IDossierGenerator
{
    Task<DossierGenerationResult> GenerateAsync(
        ResearchExecutionContext executionContext,
        EvaluationResult evaluation,
        CancellationToken cancellationToken = default);
}
