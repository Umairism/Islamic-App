using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Memory;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class WorkspaceMemoryBehavior : IResearchPipelineBehavior
{
    private readonly IMemoryRetriever _retriever;
    private readonly IMemoryRanker _ranker;
    private readonly IMemoryContextBuilder _contextBuilder;
    private readonly IResearchFeatureFlags _featureFlags;

    public WorkspaceMemoryBehavior(
        IMemoryRetriever retriever,
        IMemoryRanker ranker,
        IMemoryContextBuilder contextBuilder,
        IResearchFeatureFlags featureFlags)
    {
        _retriever = retriever;
        _ranker = ranker;
        _contextBuilder = contextBuilder;
        _featureFlags = featureFlags;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        if (!_featureFlags.EnableKnowledgeMemory)
        {
            return await next(executionContext);
        }

        // Get target workspace ID
        var workspaceId = executionContext.Metadata?.WorkspaceId ?? Guid.Empty;

        // 1. Retrieve raw workspace memories
        var query = executionContext.Context.Input.Query.OriginalRequest.Query;
        var rawMemories = await _retriever.RetrieveAsync(workspaceId, query, cancellationToken);

        // 2. Rank memories applying temporal decay filters
        var ranked = await _ranker.RankAsync(query, rawMemories, cancellationToken);

        // 3. Compact / Build Context with a strict budget options configuration (e.g. 1000 tokens)
        var options = new MemoryContextOptions(1000);
        var selectedResult = await _contextBuilder.BuildContextAsync(ranked, options, cancellationToken);

        // Attach to context
        var updatedContext = executionContext.Context with { Memory = selectedResult };
        var enrichedContext = executionContext
            .WithContext(updatedContext)
            .WithMemory(selectedResult);

        return await next(enrichedContext);
    }
}
