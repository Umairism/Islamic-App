namespace IslamicApp.Infrastructure.Persistence.Entities;

public class HadithEntity
{
    public string Id { get; set; } = string.Empty;
    public string CollectionId { get; set; } = string.Empty;
    public HadithCollectionEntity Collection { get; set; } = null!;
    public string BookId { get; set; } = string.Empty;
    public HadithBookEntity Book { get; set; } = null!;
    public string ChapterId { get; set; } = string.Empty;
    public HadithChapterEntity Chapter { get; set; } = null!;
    public int HadithNumber { get; set; }
    public string? CanonicalNumber { get; set; }
    public string? OriginalNumber { get; set; }
    public string ArabicText { get; set; } = string.Empty;
    public string ArabicCleaned { get; set; } = string.Empty;
    public string EnglishNarrator { get; set; } = string.Empty;
    public string EnglishText { get; set; } = string.Empty;
}
