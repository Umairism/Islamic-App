namespace IslamicApp.Application.Research.Evaluation.Models;

public record EvaluationWeights(
    double EvidenceCoverage = 0.35,
    double CitationAccuracy = 0.35,
    double ReasoningConsistency = 0.20,
    double SourceDiversity = 0.10
);

public class EvaluationOptions
{
    public const string SectionName = "EvaluationOptions";
    public string Version { get; set; } = "1.0.0";
    public EvaluationWeights Weights { get; set; } = new();
}
