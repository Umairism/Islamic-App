using System.Collections.Generic;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Application.Research.Analysis;

public class ResearchClaimDto
{
    public string Statement { get; set; } = string.Empty;
    public List<string> SupportingEvidence { get; set; } = new();
    public double Confidence { get; set; }
    public string ClaimType { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
}

public class ResearchFindingDto
{
    public string Section { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public List<string> CitedReferences { get; set; } = new();
}

public class ResearchLimitationDto
{
    public string LimitationDescription { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public List<string> AffectedEvidences { get; set; } = new();
}

public class ReasoningResultDto
{
    public string Summary { get; set; } = string.Empty;
    public List<ResearchClaimDto> Claims { get; set; } = new();
    public List<ResearchFindingDto> Findings { get; set; } = new();
    public List<ResearchLimitationDto> Limitations { get; set; } = new();
}

public interface IReasoningParser
{
    Result<ReasoningResult> Parse(string rawText, ResearchContext context, GenerationMetadata metadata);
}
