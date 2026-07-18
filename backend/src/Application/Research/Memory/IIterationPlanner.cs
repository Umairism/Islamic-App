using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Memory;

public interface IIterationPlanner
{
    IterationDecision Plan(
        IterationContext context,
        ValidationReport validation,
        ReasoningResult reasoning,
        ReasoningBudget budget);
}
