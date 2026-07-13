using System;
using System.Collections.Generic;
using IslamicApp.Application.DTOs;

namespace IslamicApp.Application.Research.Models;

public record SearchQuery(
    string OriginalQuery,
    SearchOptions Options
);

public record SearchOptions(
    List<string>? Languages = null,
    int Page = 1,
    int PageSize = 20,
    bool IncludeHighlights = true,
    bool IncludeReasons = true,
    bool IncludeDiagnostics = true,
    bool IncludeTranslations = true
);

public sealed record SearchExecutionContext(
    Guid SearchId,
    DateTime StartedAt,
    string OriginalQuery,
    string NormalizedQuery,
    string Language,
    string Strategy,
    string RankingChecksum,
    string SynonymChecksum,
    string AliasChecksum,
    string StopwordChecksum
);

public record SearchDiagnostics(
    double ExecutionTimeMs = 0,
    double NormalizationTimeMs = 0,
    double QueryTimeMs = 0,
    double RankingTimeMs = 0,
    double EvidenceBuildTimeMs = 0,
    int TotalMatches = 0,
    int ReturnedMatches = 0
);

public record ExportMetadata(
    DateTime GeneratedAt,
    Guid SearchId,
    string ApplicationVersion,
    string DatasetVersions,
    double ExecutionTimeMs,
    List<string> SourcesUsed,
    string Language
);

public record EvidenceReference(
    string SourceType, // e.g. "Quran"
    string Reference,  // e.g. "2:255"
    int GlobalIndex,
    string Language
);

public record SearchCandidate(
    string SourceType,
    string SourceName,
    string Reference,
    string PrimaryText,
    string OriginalLanguage,
    List<TranslationDto> Translations,
    Dictionary<string, object> Metadata
)
{
    // Temporary variables used by ranking engine
    public double Score { get; set; } = 0;
    public List<string> Reasons { get; } = new();
    public List<string> Highlights { get; } = new();
    public List<string> MatchedTerms { get; } = new();
}

public record EvidenceItem(
    string SourceType,
    string SourceName,
    string Reference,
    string PrimaryText,
    string OriginalLanguage,
    List<TranslationDto> Translations,
    Dictionary<string, object> Metadata,
    double Score,
    List<string> Reasons,
    List<string> Highlights
);

public record EvidenceDossier(
    SearchExecutionContext ExecutionContext,
    string Summary,
    List<EvidenceItem> PrimaryEvidence,
    List<EvidenceItem> SupportingEvidence,
    List<string> RelatedReferences,
    List<string> RelatedTopics,
    ExportMetadata ExportMetadata
);

public record SearchSuggestionDto(
    string Type, // "Reference", "Alias", "Surah"
    string Value
);
