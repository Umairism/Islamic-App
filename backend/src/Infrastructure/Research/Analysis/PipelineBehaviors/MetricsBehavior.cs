using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using Microsoft.Extensions.Logging;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class MetricsBehavior : IResearchPipelineBehavior
{
    private readonly ILogger<MetricsBehavior> _logger;

    public MetricsBehavior(ILogger<MetricsBehavior> logger)
    {
        _logger = logger;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var result = await next(executionContext);
        sw.Stop();

        _logger.LogInformation("Execution of stage {Stage} took {DurationMs} ms", 
            executionContext.CurrentStage, sw.ElapsedMilliseconds);

        return result;
    }
}
