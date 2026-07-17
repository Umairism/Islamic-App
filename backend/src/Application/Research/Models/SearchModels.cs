using System;
using System.Collections.Generic;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Enums;
using IslamicApp.Application.Retrieval.Diagnostics;

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
    KnowledgeIdentifier Identifier,
    string FormattedReference,
    int GlobalIndex
);

public record EvidenceItem(
    EvidenceSource Source,
    string Collection,
    string Reference,
    string PrimaryText,
    IReadOnlyList<TranslationDto> Translations,
    EvidenceMetadata Metadata,
    double Score,
    List<string> Reasons,
    List<string> Highlights,
    List<RelatedEvidence> Related
);

public record EvidenceCollection(
    string GroupName,
    List<EvidenceItem> Items
);



public record EvidenceDossier(
    SearchExecutionContext ExecutionContext,
    string Summary,
    List<EvidenceCollection> Collections,
    List<string> RelatedReferences,
    List<string> RelatedTopics,
    ExportMetadata ExportMetadata,
    List<PipelineEvent>? Traces = null
);

public record SearchSuggestionDto(
    string Type, // "Reference", "Alias", "Surah"
    string Value
);
