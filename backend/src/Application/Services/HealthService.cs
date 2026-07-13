using System;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;

namespace IslamicApp.Application.Services;

public class HealthService : IHealthService
{
    private static readonly DateTime StartTime = DateTime.UtcNow;
    private readonly IDatasetRepository _datasetRepository;
    private readonly IImportSessionRepository _importSessionRepository;

    public HealthService(
        IDatasetRepository datasetRepository,
        IImportSessionRepository importSessionRepository)
    {
        _datasetRepository = datasetRepository;
        _importSessionRepository = importSessionRepository;
    }

    public async Task<HealthCheckResponseDto> CheckHealthAsync(string correlationId, CancellationToken cancellationToken)
    {
        var dbConnected = await _datasetRepository.PingAsync(cancellationToken);
        
        var latestImport = await _importSessionRepository.GetLatestImportSessionAsync(cancellationToken);
        
        var datasetStatus = "Unhealthy";
        var latestImportStr = "None";
        
        if (latestImport != null)
        {
            latestImportStr = latestImport.CompletedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            if (latestImport.Status == "SUCCESS")
            {
                datasetStatus = "Healthy";
            }
        }

        var uptime = DateTime.UtcNow - StartTime;
        var uptimeStr = $"{(int)uptime.TotalDays:D2}:{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";

        return new HealthCheckResponseDto
        {
            Status = dbConnected && datasetStatus == "Healthy" ? "Healthy" : "Unhealthy",
            Database = dbConnected ? "Healthy" : "Unhealthy",
            Dataset = datasetStatus,
            LatestImport = latestImportStr,
            Uptime = uptimeStr,
            Version = "0.2.0",
            CorrelationId = correlationId
        };
    }
}
