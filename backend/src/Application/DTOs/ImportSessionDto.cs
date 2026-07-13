namespace IslamicApp.Application.DTOs;

public class ImportSessionDto
{
    public string Id { get; set; }
    public string DatasetId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string Status { get; set; }
    public int DurationMs { get; set; }
    public List<string> Warnings { get; set; } = new List<string>();
    public List<string> Errors { get; set; } = new List<string>();
    public double MemoryUsageMb { get; set; }
}
