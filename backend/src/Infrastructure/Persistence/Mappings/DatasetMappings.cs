using IslamicApp.Application.DTOs;
using IslamicApp.Infrastructure.Persistence.Entities;

namespace IslamicApp.Infrastructure.Persistence.Mappings;

public static class DatasetMappings
{
    public static DatasetDto ToDto(this DatasetEntity entity)
    {
        if (entity == null) return null;

        return new DatasetDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Edition = entity.Edition,
            Version = entity.Version,
            Source = entity.Source,
            License = entity.License,
            Checksum = entity.Checksum,
            ImportedAt = entity.ImportedAt
        };
    }
}
