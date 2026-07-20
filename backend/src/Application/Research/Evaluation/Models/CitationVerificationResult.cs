namespace IslamicApp.Application.Research.Evaluation.Models;

public record CitationVerificationResult(
    bool Exists,
    double RelevanceScore,
    string Explanation
);
