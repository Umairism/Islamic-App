namespace IslamicApp.Application.Research.Evaluation.Models;

public record ResearchQualityScore(
    double EvidenceCoverage,
    double CitationAccuracy,
    double ReasoningConsistency,
    double SourceDiversity,
    double OverallScore
);
