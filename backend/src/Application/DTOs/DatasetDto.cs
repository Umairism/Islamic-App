namespace IslamicApp.Application.DTOs;

public class DatasetDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Edition { get; set; }
    public string Version { get; set; }
    public string Source { get; set; }
    public string License { get; set; }
    public string Checksum { get; set; }
    public DateTime ImportedAt { get; set; }
}
