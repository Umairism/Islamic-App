using System;

namespace IslamicApp.Application.Research.Models;

public record ResearchExecutionSnapshot(
    Guid Id,
    Guid ResearchSessionId,
    string Provider,
    string Model,
    string PromptHash,
    string PromptVersion,
    string TemplateVersion,
    string ProviderParametersHash,
    string SchemaVersion,
    string CompletionHash,
    int PromptTokens,
    int CompletionTokens,
    double DurationMs,
    int RetryCount,
    DateTimeOffset CreatedAt
);
