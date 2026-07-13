using IslamicApp.Application.DTOs;
using IslamicApp.Infrastructure.Persistence.Entities;

namespace IslamicApp.Infrastructure.Persistence.Mappings;

public static class ImportSessionMappings
{
    public static ImportSessionDto ToDto(this ImportSessionEntity entity)
    {
        if (entity == null) return null;

        return new ImportSessionDto
        {
            Id = entity.Id,
            DatasetId = entity.DatasetId,
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            Status = entity.Status,
            DurationMs = entity.DurationMs,
            Warnings = entity.Warnings ?? new List<string>(),
            Errors = entity.Errors ?? new List<string>(),
            MemoryUsageMb = entity.MemoryUsageMb
        };
    }
}
