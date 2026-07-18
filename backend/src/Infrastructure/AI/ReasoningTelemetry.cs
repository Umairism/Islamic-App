using System;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using Microsoft.Extensions.Logging;

namespace IslamicApp.Infrastructure.AI;

public class ReasoningTelemetry : IReasoningTelemetry
{
    private readonly ILogger<ReasoningTelemetry> _logger;

    public ReasoningTelemetry(ILogger<ReasoningTelemetry> logger)
    {
        _logger = logger;
    }

    public void TrackUsage(GenerationMetadata metadata)
    {
        _logger.LogInformation("Reasoning usage tracked: Provider: {Provider}, Model: {Model}, PromptTokens: {PromptTokens}, CompletionTokens: {CompletionTokens}, DurationMs: {DurationMs}, Cached: {Cached}, FinishReason: {FinishReason}",
            metadata.Provider, metadata.Model, metadata.PromptTokens, metadata.CompletionTokens, metadata.Duration.TotalMilliseconds, metadata.Cached, metadata.FinishReason);
    }

    public void TrackRetry(string provider, int attempt, Exception ex)
    {
        _logger.LogWarning(ex, "Reasoning retry attempt {Attempt} for provider {Provider} due to transient exception: {ErrorMessage}",
            attempt, provider, ex.Message);
    }

    public void TrackCircuitBreak(string provider, TimeSpan duration)
    {
        _logger.LogCritical("Circuit breaker OPENED for provider {Provider} for {DurationSeconds} seconds due to repeated failures.",
            provider, duration.TotalSeconds);
    }

    public void TrackValidationFailure(ValidationReport report)
    {
        _logger.LogError("Reasoning Validation Failures tracked. Claims status: {ClaimsPassed}, Citations status: {CitationsPassed}, Consistency status: {ConsistencyPassed}",
            report.ClaimValidation.Passed, report.CitationValidation.Passed, report.ConsistencyValidation.Passed);

        foreach (var issue in report.ClaimValidation.Issues)
        {
            _logger.LogWarning("Validation Issue (Claim) [{RuleName}]: {Description} (Severity: {Severity})",
                issue.RuleName, issue.Description, issue.Severity);
        }
        foreach (var issue in report.CitationValidation.Issues)
        {
            _logger.LogWarning("Validation Issue (Citation) [{RuleName}]: {Description} (Severity: {Severity})",
                issue.RuleName, issue.Description, issue.Severity);
        }
        foreach (var issue in report.ConsistencyValidation.Issues)
        {
            _logger.LogWarning("Validation Issue (Consistency) [{RuleName}]: {Description} (Severity: {Severity})",
                issue.RuleName, issue.Description, issue.Severity);
        }
    }
}
