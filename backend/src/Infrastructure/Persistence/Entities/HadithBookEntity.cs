using System.Collections.Generic;

namespace IslamicApp.Infrastructure.Persistence.Entities;

public class HadithBookEntity
{
    public string Id { get; set; } = string.Empty;
    public string CollectionId { get; set; } = string.Empty;
    public HadithCollectionEntity Collection { get; set; } = null!;
    public int BookNumber { get; set; }
    public string TitleArabic { get; set; } = string.Empty;
    public string TitleEnglish { get; set; } = string.Empty;

    public ICollection<HadithChapterEntity> Chapters { get; set; } = new List<HadithChapterEntity>();
    public ICollection<HadithEntity> Hadiths { get; set; } = new List<HadithEntity>();
}
