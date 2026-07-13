using System.Collections.Generic;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class HadithCollectionEntity
{
    public string Id { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string TitleArabic { get; set; } = string.Empty;
    public string TitleEnglish { get; set; } = string.Empty;
    public string AuthorArabic { get; set; } = string.Empty;
    public string AuthorEnglish { get; set; } = string.Empty;
    public string IntroductionArabic { get; set; } = string.Empty;
    public string IntroductionEnglish { get; set; } = string.Empty;
    public int TotalHadiths { get; set; }

    public ICollection<HadithBookEntity> Books { get; set; } = new List<HadithBookEntity>();
    public ICollection<HadithEntity> Hadiths { get; set; } = new List<HadithEntity>();
}
