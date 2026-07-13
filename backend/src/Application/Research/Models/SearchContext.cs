using System.Collections.Generic;
using System.Collections.Immutable;

namespace IslamicApp.Application.Research.Models;

public record SearchContext(
    SearchQuery Query,
    SearchOptions Options,
    SearchExecutionContext? ExecutionContext = null,
    string NormalizedQuery = "",
    IReadOnlyList<string>? RawTokens = null,
    IReadOnlyList<string>? NormalizedTokens = null,
    IReadOnlyList<string>? UniqueTokens = null,
    IReadOnlyList<string>? ExpandedTokens = null,
    EvidenceReference? ResolvedReference = null,
    IReadOnlyList<EvidenceMatch>? Candidates = null,
    IReadOnlyList<EvidenceMatch>? RankedCandidates = null,
    IReadOnlyList<EvidenceItem>? EvidenceItems = null,
    SearchDiagnostics? Diagnostics = null
)
{
    public IReadOnlyList<string> RawTokensList => RawTokens ?? ImmutableList<string>.Empty;
    public IReadOnlyList<string> NormalizedTokensList => NormalizedTokens ?? ImmutableList<string>.Empty;
    public IReadOnlyList<string> UniqueTokensList => UniqueTokens ?? ImmutableList<string>.Empty;
    public IReadOnlyList<string> ExpandedTokensList => ExpandedTokens ?? ImmutableList<string>.Empty;
    public IReadOnlyList<EvidenceMatch> CandidatesList => Candidates ?? ImmutableList<EvidenceMatch>.Empty;
    public IReadOnlyList<EvidenceMatch> RankedCandidatesList => RankedCandidates ?? ImmutableList<EvidenceMatch>.Empty;
    public IReadOnlyList<EvidenceItem> EvidenceItemsList => EvidenceItems ?? ImmutableList<EvidenceItem>.Empty;
    public SearchDiagnostics DiagnosticsValue => Diagnostics ?? new SearchDiagnostics();
}
