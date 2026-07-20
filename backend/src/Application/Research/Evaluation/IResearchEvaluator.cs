using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Evaluation.Models;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Evaluation;

public interface IResearchEvaluator
{
    Task<EvaluationResult> EvaluateAsync(
        ResearchExecutionContext executionContext,
        CancellationToken cancellationToken = default);
}
