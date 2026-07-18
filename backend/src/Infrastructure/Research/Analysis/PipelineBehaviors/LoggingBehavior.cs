using System;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using Microsoft.Extensions.Logging;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class LoggingBehavior : IResearchPipelineBehavior
{
    private readonly ILogger<LoggingBehavior> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior> logger)
    {
        _logger = logger;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting stage {Stage} for query '{Query}'", 
            executionContext.CurrentStage, executionContext.Context.Input.Query.Query.Original);

        var result = await next(executionContext);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Successfully completed stage {Stage}", result.Value!.CurrentStage);
        }
        else
        {
            _logger.LogError("Failed stage {Stage} with error {ErrorCode}: {ErrorMessage}", 
                executionContext.CurrentStage, result.Error!.Code, result.Error!.Message);
        }

        return result;
    }
}
