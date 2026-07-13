using IslamicApp.Application.DTOs;
using IslamicApp.Infrastructure.Persistence.Entities;

namespace IslamicApp.Infrastructure.Persistence.Mappings;

public static class TranslationMappings
{
    public static TranslationDto ToDto(this QuranTranslationEntity entity)
    {
        if (entity == null) return null;

        return new TranslationDto
        {
            Language = entity.Language,
            Translator = entity.Translator,
            Text = entity.Text
        };
    }

    public static TranslationInfoDto ToInfoDto(this QuranTranslationEntity entity)
    {
        if (entity == null) return null;

        return new TranslationInfoDto
        {
            Language = entity.Language,
            Translator = entity.Translator
        };
    }
}
