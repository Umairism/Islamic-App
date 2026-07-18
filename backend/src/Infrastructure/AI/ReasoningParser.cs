using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.AI;

public class ReasoningParser : IReasoningParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public Result<ReasoningResult> Parse(string rawText, ResearchContext context, GenerationMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return Result<ReasoningResult>.Failure(new Error("EmptyOutput", "Raw model output was empty.", ErrorSeverity.Error));
        }

        var cleanedJson = CleanRawOutput(rawText);

        try
        {
            var dto = JsonSerializer.Deserialize<ReasoningResultDto>(cleanedJson, JsonOptions);
            if (dto == null)
            {
                return Result<ReasoningResult>.Failure(new Error("DeserializationFailed", "JSON deserialization returned null.", ErrorSeverity.Error));
            }

            var claims = dto.Claims?.Select(c => new ResearchClaim(
                Statement: c.Statement,
                SupportingEvidence: c.SupportingEvidence?.Select(r => new ReferenceId(r)).ToList() ?? new List<ReferenceId>(),
                Confidence: new ConfidenceScore(c.Confidence),
                ClaimType: Enum.TryParse<ClaimType>(c.ClaimType, true, out var ct) ? ct : ClaimType.Theological,
                Origin: Enum.TryParse<ClaimOrigin>(c.Origin, true, out var co) ? co : ClaimOrigin.DirectEvidence
            )).ToList() ?? new List<ResearchClaim>();

            var findings = dto.Findings?.Select(f => new ResearchFinding(
                Section: f.Section,
                Heading: f.Heading,
                Details: f.Details,
                CitedReferences: f.CitedReferences?.Select(r => new ReferenceId(r)).ToList() ?? new List<ReferenceId>()
            )).ToList() ?? new List<ResearchFinding>();

            var limitations = dto.Limitations?.Select(l => new ResearchLimitation(
                LimitationDescription: l.LimitationDescription,
                Impact: l.Impact,
                AffectedEvidences: l.AffectedEvidences?.Select(r => new ReferenceId(r)).ToList() ?? new List<ReferenceId>()
            )).ToList() ?? new List<ResearchLimitation>();

            // Map DTO to ReasoningResult domain model
            var result = new ReasoningResult(
                Summary: dto.Summary,
                Claims: claims,
                Findings: findings,
                Limitations: limitations,
                Methodology: context.Analysis?.Methodology.Type ?? ResearchMethodologyType.Thematic,
                PromptVersion: "1.0.0", // default version tracker
                RawResponse: rawText,
                Metadata: metadata
            );

            return Result<ReasoningResult>.Success(result);
        }
        catch (JsonException ex)
        {
            return Result<ReasoningResult>.Failure(new Error("InvalidJsonSchema", $"Failed to parse model output as JSON DTO. Details: {ex.Message}", ErrorSeverity.Error));
        }
        catch (Exception ex)
        {
            return Result<ReasoningResult>.Failure(new Error("ParsingError", $"An unexpected error occurred while parsing reasoning: {ex.Message}", ErrorSeverity.Error));
        }
    }

    private string CleanRawOutput(string rawText)
    {
        var cleaned = rawText.Trim();

        // 1. Strip markdown code block wrappers (e.g. ```json ... ``` or ``` ...)
        var match = Regex.Match(cleaned, @"^```(?:json)?\s*(.*?)\s*```$", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        if (match.Success)
        {
            cleaned = match.Groups[1].Value.Trim();
        }

        // 2. Fallback: Find first '{' and last '}' if there is surrounding chatter
        if (!cleaned.StartsWith("{") || !cleaned.EndsWith("}"))
        {
            var firstBrace = cleaned.IndexOf('{');
            var lastBrace = cleaned.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                cleaned = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
        }

        return cleaned;
    }
}
