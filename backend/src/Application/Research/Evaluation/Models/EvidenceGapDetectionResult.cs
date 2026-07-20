using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Evaluation.Models;

public enum ClaimRiskLevel
{
    Low,
    Moderate,
    High,
    Overreaching
}

public record EvidenceGapDetectionResult(
    ResearchClaim Claim,
    int SupportingEvidenceCount,
    ClaimRiskLevel RiskLevel,
    string WarningMessage
);
