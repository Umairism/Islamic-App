using System.Collections.Generic;
using System.Collections.Immutable;

namespace IslamicApp.Application.Research.Models;

public record SearchContext(
    SearchRequest Request,
    QueryAnalysis Analysis,
    IReadOnlyList<KnowledgeMatch>? Candidates = null,
    IReadOnlyList<KnowledgeMatch>? RankedCandidates = null,
    IReadOnlyList<ResearchEvidenceItem>? ResearchEvidenceItems = null,
    SearchDiagnostics? Diagnostics = null
)
{
    public IReadOnlyList<KnowledgeMatch> CandidatesList => Candidates ?? ImmutableList<KnowledgeMatch>.Empty;
    public IReadOnlyList<KnowledgeMatch> RankedCandidatesList => RankedCandidates ?? ImmutableList<KnowledgeMatch>.Empty;
    public IReadOnlyList<ResearchEvidenceItem> ResearchEvidenceItemsList => ResearchEvidenceItems ?? ImmutableList<ResearchEvidenceItem>.Empty;
    public SearchDiagnostics DiagnosticsValue => Diagnostics ?? new SearchDiagnostics();
}
