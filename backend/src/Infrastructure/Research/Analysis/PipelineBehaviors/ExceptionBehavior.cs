using System;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Research.Analysis;
using IslamicApp.Application.Research.Models;
using Microsoft.Extensions.Logging;

namespace IslamicApp.Infrastructure.Research.Analysis.PipelineBehaviors;

public class ExceptionBehavior : IResearchPipelineBehavior
{
    private readonly ILogger<ExceptionBehavior> _logger;

    public ExceptionBehavior(ILogger<ExceptionBehavior> logger)
    {
        _logger = logger;
    }

    public async Task<Result<ResearchExecutionContext>> HandleAsync(
        ResearchExecutionContext executionContext,
        Func<ResearchExecutionContext, Task<Result<ResearchExecutionContext>>> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(executionContext);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Research pipeline operation was cancelled.");
            return Result<ResearchExecutionContext>.Failure(new Error("OperationCancelled", "The research operation was cancelled.", ErrorSeverity.Warning));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred in the Research Pipeline.");
            var msg = ex.Message;
            if (ex.InnerException != null) msg += " INNER: " + ex.InnerException.Message;
            return Result<ResearchExecutionContext>.Failure(new Error("PipelineError", $"An unexpected error occurred: {msg}", ErrorSeverity.Critical));
        }
    }
}
