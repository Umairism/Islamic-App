using IslamicApp.Application.DTOs;
using IslamicApp.Infrastructure.Persistence.Entities;

namespace IslamicApp.Infrastructure.Persistence.Mappings;

public static class SurahMappings
{
    public static SurahDto ToDto(this SurahEntity entity)
    {
        if (entity == null) return null;

        return new SurahDto
        {
            Number = entity.Number,
            ArabicName = entity.ArabicName,
            Transliteration = entity.Transliteration,
            EnglishName = entity.EnglishName,
            RevelationType = entity.RevelationType,
            TotalVerses = entity.TotalVerses
        };
    }
}
