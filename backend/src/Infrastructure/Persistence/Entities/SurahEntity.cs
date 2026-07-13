namespace IslamicApp.Infrastructure.Persistence.Entities;

public class SurahEntity
{
    public string Id { get; set; }
    public int Number { get; set; }
    public string ArabicName { get; set; }
    public string Transliteration { get; set; }
    public string EnglishName { get; set; }
    public string RevelationType { get; set; }
    public int TotalVerses { get; set; }

    public ICollection<QuranVerseEntity> Verses { get; set; } = new List<QuranVerseEntity>();
}
