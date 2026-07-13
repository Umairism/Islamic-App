using IslamicApp.Application.DTOs;

namespace IslamicApp.Application.Common.Interfaces;

public interface IHealthService
{
    Task<HealthCheckResponseDto> CheckHealthAsync(string correlationId, CancellationToken cancellationToken);
}
