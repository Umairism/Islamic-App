namespace IslamicApp.Application.DTOs;

public class SurahDto
{
    public int Number { get; set; }
    public string ArabicName { get; set; }
    public string Transliteration { get; set; }
    public string EnglishName { get; set; }
    public string RevelationType { get; set; }
    public int TotalVerses { get; set; }
}
