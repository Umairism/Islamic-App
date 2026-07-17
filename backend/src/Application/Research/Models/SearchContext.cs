using System.Collections.Generic;
using System.Collections.Immutable;
using IslamicApp.Application.Retrieval.Diagnostics;
using IslamicApp.Application.Retrieval.Hybrid;

namespace IslamicApp.Application.Research.Models;

public record SearchContext(
    SearchRequest Request,
    QueryAnalysis Analysis,
    IReadOnlyList<KnowledgeMatch>? Candidates = null,
    IReadOnlyList<KnowledgeMatch>? RankedCandidates = null,
    IReadOnlyList<ResearchEvidenceItem>? ResearchEvidenceItems = null,
    SearchDiagnostics? Diagnostics = null,
    IReadOnlyList<CandidateDocument>? RetrievedCandidates = null,
    IReadOnlyList<PipelineEvent>? Traces = null
)
{
    public IReadOnlyList<KnowledgeMatch> CandidatesList => Candidates ?? ImmutableList<KnowledgeMatch>.Empty;
    public IReadOnlyList<KnowledgeMatch> RankedCandidatesList => RankedCandidates ?? ImmutableList<KnowledgeMatch>.Empty;
    public IReadOnlyList<ResearchEvidenceItem> ResearchEvidenceItemsList => ResearchEvidenceItems ?? ImmutableList<ResearchEvidenceItem>.Empty;
    public SearchDiagnostics DiagnosticsValue => Diagnostics ?? new SearchDiagnostics();
    public IReadOnlyList<CandidateDocument> RetrievedCandidatesList => RetrievedCandidates ?? ImmutableList<CandidateDocument>.Empty;
    public IReadOnlyList<PipelineEvent> TracesList => Traces ?? ImmutableList<PipelineEvent>.Empty;
}
