using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Interfaces;
using IslamicApp.Application.Research.Models;

namespace IslamicApp.Infrastructure.Search;

public class SearchPipeline : ISearchPipeline
{
    private readonly List<ISearchPipelineStage> _stages = new();

    public SearchPipeline(
        DatabaseQueryStage databaseQueryStage,
        RankingStage rankingStage,
        EvidenceBuildStage evidenceBuildStage)
    {
        _stages.Add(databaseQueryStage);
        _stages.Add(rankingStage);
        _stages.Add(evidenceBuildStage);
    }

    public async Task<SearchContext> ExecuteAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var currentContext = context;
        foreach (var stage in _stages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            currentContext = await stage.ExecuteAsync(currentContext, cancellationToken);
        }
        return currentContext;
    }

    public async Task<ProfilerResult> ExecuteWithProfilingAsync(SearchContext context, CancellationToken cancellationToken)
    {
        var timeline = new List<PipelineProfilerStep>();
        var currentContext = context;
        
        foreach (var stage in _stages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            long startMemory = GC.GetAllocatedBytesForCurrentThread();
            var sw = Stopwatch.StartNew();
            string status = "Success";
            
            try
            {
                currentContext = await stage.ExecuteAsync(currentContext, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                status = "Cancelled";
                throw;
            }
            catch (Exception ex)
            {
                status = $"Exception: {ex.Message}";
                throw;
            }
            finally
            {
                sw.Stop();
                long endMemory = GC.GetAllocatedBytesForCurrentThread();
                timeline.Add(new PipelineProfilerStep(
                    StageName: stage.GetType().Name,
                    DurationMs: sw.Elapsed.TotalMilliseconds,
                    MemoryDeltaBytes: endMemory - startMemory,
                    Status: status
                ));
            }
        }
        
        return new ProfilerResult(currentContext, timeline);
    }
}
