using System.Collections.Generic;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public interface IValidationRule
{
    string RuleName { get; }
    IEnumerable<ValidationIssue> Evaluate(ReasoningResult reasoning, ResearchContext context);
}
