using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search;

public class DatabaseQueryStage : ISearchPipelineStage
{
    private readonly IRetrievalOrchestrator _orchestrator;

    public DatabaseQueryStage(IRetrievalOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        
        var matches = await _orchestrator.RetrieveMatchesAsync(context.Analysis, cancellationToken);
        
        sw.Stop();

        var updatedDiagnostics = context.DiagnosticsValue with 
        { 
            QueryTimeMs = sw.Elapsed.TotalMilliseconds,
            TotalMatches = matches.Count
        };

        return context with
        {
            Candidates = matches,
            Diagnostics = updatedDiagnostics
        };
    }
}

public class RankingStage : ISearchPipelineStage
{
    private readonly IRankingEngine _rankingEngine;

    public RankingStage(IRankingEngine rankingEngine)
    {
        _rankingEngine = rankingEngine;
    }

    public Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var updatedContext = _rankingEngine.Rank(context);
        sw.Stop();

        var updatedDiagnostics = updatedContext.DiagnosticsValue with 
        { 
            RankingTimeMs = sw.Elapsed.TotalMilliseconds 
        };

        return Task.FromResult(updatedContext with 
        { 
            Diagnostics = updatedDiagnostics
        });
    }
}

public class EvidenceBuildStage : ISearchPipelineStage
{
    private readonly IEvidenceBuilder _evidenceBuilder;
    private readonly ICrossReferenceEngine _crossReferenceEngine;

    public EvidenceBuildStage(IEvidenceBuilder evidenceBuilder, ICrossReferenceEngine crossReferenceEngine)
    {
        _evidenceBuilder = evidenceBuilder;
        _crossReferenceEngine = crossReferenceEngine;
    }

    public async Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();

        var page = context.Request.Pagination.Page;
        var pageSize = context.Request.Pagination.PageSize;

        var paginatedMatches = context.RankedCandidatesList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var researchItems = new List<ResearchEvidenceItem>();
        foreach (var match in paginatedMatches)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var crossRefs = await _crossReferenceEngine.ResolveReferencesAsync(match.Document.Source, match.Document.Reference.LookupKey, cancellationToken);
            researchItems.Add(_evidenceBuilder.BuildResearchItem(match, crossRefs));
        }

        sw.Stop();

        var updatedDiagnostics = context.DiagnosticsValue with 
        { 
            EvidenceBuildTimeMs = sw.Elapsed.TotalMilliseconds,
            ReturnedMatches = researchItems.Count
        };

        return context with
        {
            ResearchEvidenceItems = researchItems,
            Diagnostics = updatedDiagnostics
        };
    }
}
