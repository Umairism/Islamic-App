using System;
using System.Collections.Generic;
using System.Linq;
using IslamicApp.Application.DTOs;
using IslamicApp.Infrastructure.Persistence.Entities;

namespace IslamicApp.Infrastructure.Persistence.Mappings;

public static class VerseMappings
{
    public static VerseDto ToDto(this QuranVerseEntity entity, IEnumerable<string> languages = null)
    {
        if (entity == null) return null;

        var translations = entity.Translations ?? new List<QuranTranslationEntity>();
        
        if (languages != null && languages.Any())
        {
            var langSet = new HashSet<string>(languages, StringComparer.OrdinalIgnoreCase);
            translations = translations.Where(t => langSet.Contains(t.Language)).ToList();
        }

        return new VerseDto
        {
            Reference = new ReferenceDto
            {
                Type = "Quran",
                Reference = $"{entity.SurahNumber}:{entity.AyahNumber}",
                GlobalIndex = entity.GlobalIndex,
                Language = "ar"
            },
            SurahNumber = entity.SurahNumber,
            AyahNumber = entity.AyahNumber,
            ArabicText = entity.ArabicText,
            ArabicCleaned = entity.ArabicCleaned,
            Transliteration = entity.Transliteration,
            Translations = translations.Select(t => t.ToDto()).ToList()
        };
    }
}
