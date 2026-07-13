namespace IslamicApp.Application.DTOs;

public class VerseDto
{
    public ReferenceDto Reference { get; set; }
    public int SurahNumber { get; set; }
    public int AyahNumber { get; set; }
    public string ArabicText { get; set; }
    public string ArabicCleaned { get; set; }
    public string Transliteration { get; set; }
    public IEnumerable<TranslationDto> Translations { get; set; } = new List<TranslationDto>();
}
