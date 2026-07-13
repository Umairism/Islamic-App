using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using IslamicApp.Application.Common.Interfaces;
using IslamicApp.Application.DTOs;

namespace IslamicApp.WebApi.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    /// <summary>
    /// Check backend health parameters, database connectivity, and dataset status.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthCheckResponseDto), 200)]
    [ProducesResponseType(typeof(HealthCheckResponseDto), 503)]
    public async Task<IActionResult> GetHealth(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.Items.TryGetValue("CorrelationId", out var cid) ? cid?.ToString() : string.Empty;
        var response = await _healthService.CheckHealthAsync(correlationId, cancellationToken);
        
        if (response.Status == "Healthy")
        {
            return Ok(response);
        }
        
        return StatusCode(503, response);
    }
}
