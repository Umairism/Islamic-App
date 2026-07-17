using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;
using IslamicApp.Application.Retrieval.Diagnostics;
using IslamicApp.Application.Retrieval.Policies;
using IslamicApp.Application.Retrieval.Hybrid;

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
        
        var policy = context.Request.SemanticSearchEnabled ? SemanticPolicy.Adaptive : SemanticPolicy.LexicalOnly;
        
        var retrievalCtx = new RetrievalContext(
            Query: context.Analysis,
            Policy: policy,
            CancellationToken: cancellationToken,
            Events: ImmutableList<PipelineEvent>.Empty
        );

        var (candidates, updatedCtx) = await _orchestrator.RetrieveCandidatesAsync(retrievalCtx);
        
        sw.Stop();

        var updatedDiagnostics = context.DiagnosticsValue with 
        { 
            QueryTimeMs = sw.Elapsed.TotalMilliseconds,
            TotalMatches = candidates.Count
        };

        return context with
        {
            RetrievedCandidates = candidates,
            Traces = updatedCtx.Events,
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
