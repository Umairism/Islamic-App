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
    }
}
