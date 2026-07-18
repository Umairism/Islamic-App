using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IslamicApp.Application.Research.Analysis;

namespace IslamicApp.Infrastructure.Research;

public record ExportJob(Guid JobId, Guid WorkspaceId, string Format);

public class BackgroundJobScheduler : BackgroundService, IBackgroundJobScheduler
{
    private readonly ConcurrentQueue<ExportJob> _queue = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundJobScheduler> _logger;

    public BackgroundJobScheduler(IServiceProvider serviceProvider, ILogger<BackgroundJobScheduler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Guid EnqueueExportJob(Guid workspaceId, string format)
    {
        var jobId = Guid.NewGuid();
        _queue.Enqueue(new ExportJob(jobId, workspaceId, format));
        _logger.LogInformation("Enqueued export job {JobId} for workspace {WorkspaceId} in format {Format}", jobId, workspaceId, format);
        return jobId;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundJobScheduler started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var job))
            {
                try
                {
                    await ProcessJobAsync(job, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to run background export job {JobId}", job.JobId);
                }
            }
            else
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task ProcessJobAsync(ExportJob job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing export job {JobId}...", job.JobId);
        using var scope = _serviceProvider.CreateScope();
        var exportService = scope.ServiceProvider.GetRequiredService<WorkspaceExportService>();
        await exportService.ExecuteExportAsync(job.WorkspaceId, job.Format, cancellationToken);
        _logger.LogInformation("Finished export job {JobId}.", job.JobId);
    }
}
