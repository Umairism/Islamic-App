namespace IslamicApp.Infrastructure.Persistence.Entities;

public class QuranVerseEntity
{
    public string Id { get; set; }
    public int GlobalIndex { get; set; }
    public int SurahNumber { get; set; }
    public SurahEntity Surah { get; set; }
    public int AyahNumber { get; set; }
    public string ArabicText { get; set; }
    public string ArabicCleaned { get; set; }
    public string Transliteration { get; set; }

    public ICollection<QuranTranslationEntity> Translations { get; set; } = new List<QuranTranslationEntity>();
}
