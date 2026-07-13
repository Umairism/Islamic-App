namespace IslamicApp.Infrastructure.Persistence.Entities;

public class QuranTranslationEntity
{
    public string Id { get; set; }
    public string VerseId { get; set; }
    public QuranVerseEntity Verse { get; set; }
    public string Language { get; set; }
    public string Translator { get; set; }
    public string Text { get; set; }
}
