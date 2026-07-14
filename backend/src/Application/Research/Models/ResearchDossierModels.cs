using System;
using System.Collections.Generic;
using IslamicApp.Application.DTOs;
using IslamicApp.Application.Research.Enums;

namespace IslamicApp.Application.Research.Models;

public record EvidenceConfidence(
    string SourceAuthority,
    double TextMatch,
    double ReferenceMatch,
    double RankingScore,
    double OverallConfidence
);

public record SearchExplanation(
    List<string> TokenMatches,
    List<string> ReferenceMatches,
    List<string> Boosts,
    List<string> Penalties,
    Dictionary<string, double> RankingFactors
);

public record ResearchProvenance(
    string DatasetId,
    string ImportSessionId,
    string DatasetName,
    string Version,
    string Checksum
);

public record PipelineProfilerStep(
    string StageName,
    double DurationMs,
    long MemoryDeltaBytes,
    string Status
);

public record ProfilerResult(
    SearchContext Context,
    List<PipelineProfilerStep> Timeline
);

public record CrossReferenceItem(
    EvidenceSource Source,
    string Reference,
    EvidenceRelationshipType Relationship,
    string Description
);

public record ResearchEvidenceItem(
    EvidenceSource Source,
    string Collection,
    string Reference,
    string PrimaryText,
    IReadOnlyList<TranslationDto> Translations,
    string DatasetId,
    string ImportSessionId,
    EvidenceConfidence Confidence,
    SearchExplanation Explanation,
    List<CrossReferenceItem> CrossReferences
);

public record ResearchDossier(
    string Query,
    string Summary,
    Dictionary<EvidenceSection, List<ResearchEvidenceItem>> EvidenceSections,
    List<PipelineProfilerStep> PipelineTimeline,
    SearchDiagnostics Diagnostics,
    List<ResearchProvenance> ProvenanceList,
    ExportMetadata ExportMetadata
);
