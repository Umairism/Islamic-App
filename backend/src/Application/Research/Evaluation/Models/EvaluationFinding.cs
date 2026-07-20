using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Evaluation.Models;

public record EvaluationFinding(
    string Category,
    string Description,
    ErrorSeverity Severity
);
