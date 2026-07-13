namespace IslamicApp.Application.DTOs;

public class HealthCheckResponseDto
{
    public string Status { get; set; }
    public string Database { get; set; }
    public string Dataset { get; set; }
    public string LatestImport { get; set; }
    public string Uptime { get; set; }
    public string Version { get; set; }
    public string CorrelationId { get; set; }
}
