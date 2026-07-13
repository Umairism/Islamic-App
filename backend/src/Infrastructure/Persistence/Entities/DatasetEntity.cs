namespace IslamicApp.Infrastructure.Persistence.Entities;

public class DatasetEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Edition { get; set; }
    public string Version { get; set; }
    public string Source { get; set; }
    public string License { get; set; }
    public string Checksum { get; set; }
    public DateTime ImportedAt { get; set; }

    public ICollection<ImportSessionEntity> Sessions { get; set; } = new List<ImportSessionEntity>();
}
