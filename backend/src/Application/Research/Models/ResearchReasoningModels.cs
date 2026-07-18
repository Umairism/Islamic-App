using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.Research.Analysis;

namespace IslamicApp.Application.Research.Models;

// ==========================================
// Prompt & Variable Models
// ==========================================

public record PromptTemplate(
    string Name,
    string Version,
    string FilePath,
    IReadOnlyDictionary<string, string> ConfigMetadata
);

public record EvidenceSnippet(
    NodeId NodeId,
    DocumentId DocumentId,
    ReferenceId Reference,
    string Content,
    EvidenceClassification Classification
);

public record PromptVariables(
    string Query,
    ResearchMethodologyType Methodology,
    IReadOnlyList<EvidenceSnippet> Snippets,
    IReadOnlyList<ReferenceId> AllowedReferences
);

public record ResearchPrompt(
    PromptTemplate Template,
    PromptVariables Variables,
    string RenderedSystemPrompt,
    string RenderedUserPrompt
);

// ==========================================
// Generation options & session DTOs
// ==========================================

public enum ResponseFormat
{
    Text,
    Json,
    Markdown
}

public enum FinishReason
{
    Stop,
    Length,
    ContentFilter,
    ToolCall,
    Error
}

public record GenerationOptions(
    double Temperature = 0.2,
    double TopP = 0.9,
    int MaxTokens = 4096,
    ResponseFormat Format = ResponseFormat.Json,
    int? Seed = null
);

public record GenerationMetadata(
    string Provider,
    string Model,
    int PromptTokens,
    int CompletionTokens,
    TimeSpan Duration,
    bool Cached,
    FinishReason FinishReason
);

public record GenerationResponse(
    string Content,
    GenerationMetadata Metadata
);

public record ReasoningSession(
    Guid SessionId,
    ResearchPrompt Prompt,
    GenerationResponse Response,
    GenerationMetadata Metadata,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt
);

// ==========================================
// Claims, Findings & Limitations
// ==========================================

public enum ClaimType
{
    Theological,
    LegalRuling,
    HistoricalFact,
    LinguisticAnalysis,
    GeneralEthics
}

public enum ClaimOrigin
{
    DirectEvidence,
    MultiEvidenceInference,
    ModelInference,
    ExternalKnowledge
}

public record ResearchClaim(
    string Statement,
    IReadOnlyList<ReferenceId> SupportingEvidence,
    ConfidenceScore Confidence,
    ClaimType ClaimType,
    ClaimOrigin Origin
);

public record ResearchFinding(
    string Section,
    string Heading,
    string Details,
    IReadOnlyList<ReferenceId> CitedReferences
);

public record ResearchLimitation(
    string LimitationDescription,
    string Impact,
    IReadOnlyList<ReferenceId> AffectedEvidences
);

public record ReasoningResult(
    string Summary,
    IReadOnlyList<ResearchClaim> Claims,
    IReadOnlyList<ResearchFinding> Findings,
    IReadOnlyList<ResearchLimitation> Limitations,
    ResearchMethodologyType Methodology,
    string PromptVersion,
    string RawResponse,
    GenerationMetadata Metadata
);

// ==========================================
// Validation & Telemetry Reports
// ==========================================

public record ValidationIssue(
    string RuleName,
    string Description,
    ErrorSeverity Severity,
    IReadOnlyList<ReferenceId> AffectedReferences
);

public record ClaimValidationReport(IReadOnlyList<ValidationIssue> Issues)
{
    public bool Passed => Issues.All(i => i.Severity != ErrorSeverity.Error && i.Severity != ErrorSeverity.Critical);
}

public record CitationValidationReport(IReadOnlyList<ValidationIssue> Issues)
{
    public bool Passed => Issues.All(i => i.Severity != ErrorSeverity.Error && i.Severity != ErrorSeverity.Critical);
}

public record ConsistencyValidationReport(IReadOnlyList<ValidationIssue> Issues)
{
    public bool Passed => Issues.All(i => i.Severity != ErrorSeverity.Error && i.Severity != ErrorSeverity.Critical);
}

public record ValidationReport(
    ClaimValidationReport ClaimValidation,
    CitationValidationReport CitationValidation,
    ConsistencyValidationReport ConsistencyValidation
)
{
    public bool Passed => ClaimValidation.Passed && CitationValidation.Passed && ConsistencyValidation.Passed;
}

// ==========================================
// Trace & Rendering Results
// ==========================================

public record SourceTraceLink(
    string Sentence,
    IReadOnlyList<NodeId> EvidencePath,
    ConfidenceScore TraceConfidence
);

public record ExplainabilityMap(
    IReadOnlyList<SourceTraceLink> Traces
);

public record RenderResult(
    string Content,
    string ContentType,
    string Extension,
    string FileName
);

public record ResearchResult(
    ResearchExecutionContext ExecutionContext,
    ReasoningSession Session,
    ReasoningResult Reasoning,
    ValidationReport Validation,
    ExplainabilityMap Explainability,
    IReadOnlyList<RenderResult> Outputs
);
