using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IExplainabilityBuilder
{
    ExplainabilityMap BuildMap(ReasoningResult reasoning, ResearchContext context);
}
