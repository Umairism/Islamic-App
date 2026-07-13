using System.Collections.Generic;

namespace IslamicApp.Application.Research.Models;

public class SearchContext
{
    public SearchQuery Query { get; set; }
    public SearchOptions Options { get; set; }
    public SearchExecutionContext? ExecutionContext { get; set; }
    public string NormalizedQuery { get; set; } = string.Empty;
    public List<string> RawTokens { get; set; } = new();
    public List<string> NormalizedTokens { get; set; } = new();
    public List<string> UniqueTokens { get; set; } = new();
    public List<string> ExpandedTokens { get; set; } = new();
    public EvidenceReference? ResolvedReference { get; set; }
    public List<SearchCandidate> Candidates { get; set; } = new();
    public List<SearchCandidate> RankedCandidates { get; set; } = new();
    public List<EvidenceItem> EvidenceItems { get; set; } = new();
    public SearchDiagnostics Diagnostics { get; set; } = new();

    public SearchContext(SearchQuery query, SearchOptions options)
    {
        Query = query;
        Options = options;
    }
}
