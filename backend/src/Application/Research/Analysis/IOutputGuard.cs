using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IOutputGuard
{
    Result<ResearchResult> EvaluatePublishability(
        ResearchExecutionContext executionContext,
        ReasoningSession session,
        ReasoningResult reasoning,
        ValidationReport validation,
        ExplainabilityMap explainability);
}
