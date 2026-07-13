using Microsoft.EntityFrameworkCore;
using IslamicApp.Infrastructure.Persistence.Entities;

namespace IslamicApp.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<DatasetEntity> Datasets { get; set; }
    public DbSet<ImportSessionEntity> ImportSessions { get; set; }
    public DbSet<SurahEntity> Surahs { get; set; }
    public DbSet<QuranVerseEntity> QuranVerses { get; set; }
    public DbSet<QuranTranslationEntity> QuranTranslations { get; set; }
    public DbSet<HadithCollectionEntity> HadithCollections { get; set; }
    public DbSet<HadithBookEntity> HadithBooks { get; set; }
    public DbSet<HadithChapterEntity> HadithChapters { get; set; }
    public DbSet<HadithEntity> Hadiths { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DatasetEntity>(entity =>
        {
            entity.ToTable("Dataset");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Edition).HasColumnName("edition").IsRequired();
            entity.Property(e => e.Version).HasColumnName("version").IsRequired();
            entity.Property(e => e.Source).HasColumnName("source").IsRequired();
            entity.Property(e => e.License).HasColumnName("license").IsRequired();
            entity.Property(e => e.Checksum).HasColumnName("checksum").IsRequired();
            entity.Property(e => e.ImportedAt).HasColumnName("importedAt").IsRequired();
        });

        modelBuilder.Entity<ImportSessionEntity>(entity =>
        {
            entity.ToTable("ImportSession");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.DatasetId).HasColumnName("datasetId").IsRequired();
            entity.Property(e => e.StartedAt).HasColumnName("startedAt").IsRequired();
            entity.Property(e => e.CompletedAt).HasColumnName("completedAt").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.DurationMs).HasColumnName("durationMs").IsRequired();
            entity.Property(e => e.Warnings).HasColumnName("warnings").IsRequired();
            entity.Property(e => e.Errors).HasColumnName("errors").IsRequired();
            entity.Property(e => e.MemoryUsageMb).HasColumnName("memoryUsageMb").IsRequired();

            entity.HasOne(d => d.Dataset)
                .WithMany(p => p.Sessions)
                .HasForeignKey(d => d.DatasetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SurahEntity>(entity =>
        {
            entity.ToTable("Surah");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Number).HasColumnName("number").IsRequired();
            entity.Property(e => e.ArabicName).HasColumnName("arabicName").IsRequired();
            entity.Property(e => e.Transliteration).HasColumnName("transliteration").IsRequired();
            entity.Property(e => e.EnglishName).HasColumnName("englishName").IsRequired();
            entity.Property(e => e.RevelationType).HasColumnName("revelationType").IsRequired();
            entity.Property(e => e.TotalVerses).HasColumnName("totalVerses").IsRequired();

            entity.HasIndex(e => e.Number).IsUnique();
        });

        modelBuilder.Entity<QuranVerseEntity>(entity =>
        {
            entity.ToTable("QuranVerse");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.GlobalIndex).HasColumnName("globalIndex").IsRequired();
            entity.Property(e => e.SurahNumber).HasColumnName("surahNumber").IsRequired();
            entity.Property(e => e.AyahNumber).HasColumnName("ayahNumber").IsRequired();
            entity.Property(e => e.ArabicText).HasColumnName("arabicText").IsRequired();
            entity.Property(e => e.ArabicCleaned).HasColumnName("arabicCleaned").IsRequired();
            entity.Property(e => e.Transliteration).HasColumnName("transliteration").IsRequired();

            entity.HasIndex(e => e.GlobalIndex).IsUnique();
            entity.HasIndex(e => new { e.SurahNumber, e.AyahNumber }).IsUnique();

            entity.HasOne(d => d.Surah)
                .WithMany(p => p.Verses)
                .HasForeignKey(d => d.SurahNumber)
                .HasPrincipalKey(p => p.Number)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuranTranslationEntity>(entity =>
        {
            entity.ToTable("QuranTranslation");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.VerseId).HasColumnName("verseId").IsRequired();
            entity.Property(e => e.Language).HasColumnName("language").IsRequired();
            entity.Property(e => e.Translator).HasColumnName("translator").IsRequired();
            entity.Property(e => e.Text).HasColumnName("text").IsRequired();

            entity.HasIndex(e => new { e.VerseId, e.Language });

            entity.HasOne(d => d.Verse)
                .WithMany(p => p.Translations)
                .HasForeignKey(d => d.VerseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HadithCollectionEntity>(entity =>
        {
            entity.ToTable("HadithCollection");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Slug).HasColumnName("slug").IsRequired();
            entity.Property(e => e.ShortName).HasColumnName("shortName").IsRequired();
            entity.Property(e => e.DisplayName).HasColumnName("displayName").IsRequired();
            entity.Property(e => e.TitleArabic).HasColumnName("titleArabic").IsRequired();
            entity.Property(e => e.TitleEnglish).HasColumnName("titleEnglish").IsRequired();
            entity.Property(e => e.AuthorArabic).HasColumnName("authorArabic").IsRequired();
            entity.Property(e => e.AuthorEnglish).HasColumnName("authorEnglish").IsRequired();
            entity.Property(e => e.IntroductionArabic).HasColumnName("introductionArabic").IsRequired();
            entity.Property(e => e.IntroductionEnglish).HasColumnName("introductionEnglish").IsRequired();
            entity.Property(e => e.TotalHadiths).HasColumnName("totalHadiths").IsRequired();

            entity.HasIndex(e => e.Slug).IsUnique();
        });

        modelBuilder.Entity<HadithBookEntity>(entity =>
        {
            entity.ToTable("HadithBook");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.CollectionId).HasColumnName("collectionId").IsRequired();
            entity.Property(e => e.BookNumber).HasColumnName("bookNumber").IsRequired();
            entity.Property(e => e.TitleArabic).HasColumnName("titleArabic").IsRequired();
            entity.Property(e => e.TitleEnglish).HasColumnName("titleEnglish").IsRequired();

            entity.HasOne(d => d.Collection)
                .WithMany(p => p.Books)
                .HasForeignKey(d => d.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HadithChapterEntity>(entity =>
        {
            entity.ToTable("HadithChapter");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.BookId).HasColumnName("bookId").IsRequired();
            entity.Property(e => e.ChapterNumber).HasColumnName("chapterNumber").IsRequired();
            entity.Property(e => e.TitleArabic).HasColumnName("titleArabic").IsRequired();
            entity.Property(e => e.TitleEnglish).HasColumnName("titleEnglish").IsRequired();

            entity.HasOne(d => d.Book)
                .WithMany(p => p.Chapters)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HadithEntity>(entity =>
        {
            entity.ToTable("Hadith");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.CollectionId).HasColumnName("collectionId").IsRequired();
            entity.Property(e => e.BookId).HasColumnName("bookId").IsRequired();
            entity.Property(e => e.ChapterId).HasColumnName("chapterId").IsRequired();
            entity.Property(e => e.HadithNumber).HasColumnName("hadithNumber").IsRequired();
            entity.Property(e => e.CanonicalNumber).HasColumnName("canonicalNumber");
            entity.Property(e => e.OriginalNumber).HasColumnName("originalNumber");
            entity.Property(e => e.ArabicText).HasColumnName("arabicText").IsRequired();
            entity.Property(e => e.ArabicCleaned).HasColumnName("arabicCleaned").IsRequired();
            entity.Property(e => e.EnglishNarrator).HasColumnName("englishNarrator").IsRequired();
            entity.Property(e => e.EnglishText).HasColumnName("englishText").IsRequired();

            entity.HasIndex(e => new { e.CollectionId, e.HadithNumber });

            entity.HasOne(d => d.Collection)
                .WithMany(p => p.Hadiths)
                .HasForeignKey(d => d.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Book)
                .WithMany(p => p.Hadiths)
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Chapter)
                .WithMany(p => p.Hadiths)
                .HasForeignKey(d => d.ChapterId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
