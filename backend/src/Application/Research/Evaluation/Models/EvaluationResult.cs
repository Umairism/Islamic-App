using System;
using System.Collections.Generic;

namespace IslamicApp.Application.Research.Evaluation.Models;

public record EvaluationResult(
    Guid ResearchSessionId,
    ResearchQualityScore Score,
    IReadOnlyList<EvaluationFinding> Findings,
    string EvaluationVersion,
    DateTimeOffset EvaluatedAt
);
