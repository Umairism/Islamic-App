using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IResearchValidator
{
    ValidationReport ValidateAll(ReasoningResult reasoning, ResearchContext context);
}
