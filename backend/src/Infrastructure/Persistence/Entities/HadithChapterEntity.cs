using System.Collections.Generic;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class HadithChapterEntity
{
    public string Id { get; set; } = string.Empty;
    public string BookId { get; set; } = string.Empty;
    public HadithBookEntity Book { get; set; } = null!;
    public int ChapterNumber { get; set; }
    public string TitleArabic { get; set; } = string.Empty;
    public string TitleEnglish { get; set; } = string.Empty;

    public ICollection<HadithEntity> Hadiths { get; set; } = new List<HadithEntity>();
}
