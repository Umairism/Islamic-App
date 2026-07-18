using Microsoft.EntityFrameworkCore;
using IslamicApp.Infrastructure.Persistence.Entities;
using IslamicApp.Infrastructure.Persistence.Outbox;

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
    public DbSet<WorkspaceEntity> Workspaces { get; set; }
    public DbSet<ResearchSessionEntity> ResearchSessions { get; set; }
    public DbSet<ResearchDocumentEntity> ResearchDocuments { get; set; }
    public DbSet<DocumentRevisionEntity> DocumentRevisions { get; set; }
    public DbSet<EvidenceSnapshotEntity> EvidenceSnapshots { get; set; }
    public DbSet<ResearchExecutionSnapshotEntity> ExecutionSnapshots { get; set; }
    public DbSet<CitationEntity> Citations { get; set; }
    public DbSet<BookmarkEntity> Bookmarks { get; set; }
    public DbSet<ResearchNoteEntity> ResearchNotes { get; set; }
    public DbSet<ResearchCollectionEntity> ResearchCollections { get; set; }
    public DbSet<ResearchSessionCollectionEntity> ResearchSessionCollections { get; set; }
    public DbSet<AuditRecordEntity> AuditRecords { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<SearchIndexEntity> SearchIndices { get; set; }
    public DbSet<MemoryEntryEntity> MemoryEntries { get; set; }
    public DbSet<ResearchIterationEntity> ResearchIterations { get; set; }
    public DbSet<ResearchEventEntity> ResearchEvents { get; set; }
    public DbSet<ResearchResultEntity> ResearchResults { get; set; }

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

        modelBuilder.Entity<WorkspaceEntity>(entity =>
        {
            entity.ToTable("Workspace");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt").IsRequired();
        });

        modelBuilder.Entity<ResearchSessionEntity>(entity =>
        {
            entity.ToTable("ResearchSession");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.WorkspaceId).HasColumnName("workspaceId").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Query).HasColumnName("query").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt").IsRequired();
            entity.Property(e => e.Methodology).HasColumnName("methodology").IsRequired();
            entity.Property(e => e.Language).HasColumnName("language").IsRequired();
            entity.Property(e => e.ConfidenceValue).HasColumnName("confidenceValue").IsRequired();
            entity.Property(e => e.ConfidenceLevel).HasColumnName("confidenceLevel").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").IsRequired();
            entity.Property(e => e.CurrentStage).HasColumnName("currentStage").IsRequired();
            entity.Property(e => e.RowVersion).HasColumnName("rowVersion").IsRowVersion();

            entity.HasOne(d => d.Workspace)
                .WithMany(p => p.Sessions)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResearchDocumentEntity>(entity =>
        {
            entity.ToTable("ResearchDocument");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.SessionId).HasColumnName("sessionId").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.WorkspaceId).HasColumnName("workspaceId").IsRequired();
            entity.Property(e => e.CurrentRevisionId).HasColumnName("currentRevisionId");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt").IsRequired();

            entity.HasOne(d => d.Session)
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentRevisionEntity>(entity =>
        {
            entity.ToTable("DocumentRevision");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.DocumentId).HasColumnName("documentId").IsRequired();
            entity.Property(e => e.RevisionNumber).HasColumnName("revisionNumber").IsRequired();
            entity.Property(e => e.ParentRevisionId).HasColumnName("parentRevisionId");
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt").IsRequired();
            entity.Property(e => e.Summary).HasColumnName("summary").IsRequired();
            entity.Property(e => e.Markdown).HasColumnName("markdown").IsRequired();
            entity.Property(e => e.Html).HasColumnName("html").IsRequired();
            entity.Property(e => e.Json).HasColumnName("json").IsRequired();
            entity.Property(e => e.DiffSummary).HasColumnName("diffSummary").IsRequired();
            entity.Property(e => e.ReasoningSessionId).HasColumnName("reasoningSessionId");
            entity.Property(e => e.ExecutionSnapshotId).HasColumnName("executionSnapshotId");
            entity.Property(e => e.GeneratedBy).HasColumnName("generatedBy").IsRequired();
            entity.Property(e => e.GenerationType).HasColumnName("generationType").IsRequired();

            entity.HasOne(d => d.Document)
                .WithMany(p => p.Revisions)
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EvidenceSnapshotEntity>(entity =>
        {
            entity.ToTable("EvidenceSnapshot");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.ResearchSessionId).HasColumnName("researchSessionId").IsRequired();
            entity.Property(e => e.EvidenceNodeId).HasColumnName("evidenceNodeId").IsRequired();
            entity.Property(e => e.RetrievalScore).HasColumnName("retrievalScore").IsRequired();
            entity.Property(e => e.Confidence).HasColumnName("confidence").IsRequired();
            entity.Property(e => e.Rank).HasColumnName("rank").IsRequired();
            entity.Property(e => e.SnapshotHash).HasColumnName("snapshotHash").IsRequired();
            entity.Property(e => e.DatasetVersion).HasColumnName("datasetVersion").IsRequired();
            entity.Property(e => e.KnowledgeBaseVersion).HasColumnName("knowledgeBaseVersion").IsRequired();
            entity.Property(e => e.IndexerVersion).HasColumnName("indexerVersion").IsRequired();
            entity.Property(e => e.RetrievedAt).HasColumnName("retrievedAt").IsRequired();

            entity.HasOne(d => d.Session)
                .WithMany(p => p.EvidenceSnapshots)
                .HasForeignKey(d => d.ResearchSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResearchExecutionSnapshotEntity>(entity =>
        {
            entity.ToTable("ResearchExecutionSnapshot");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.ResearchSessionId).HasColumnName("researchSessionId").IsRequired();
            entity.Property(e => e.Provider).HasColumnName("provider").IsRequired();
            entity.Property(e => e.Model).HasColumnName("model").IsRequired();
            entity.Property(e => e.PromptHash).HasColumnName("promptHash").IsRequired();
            entity.Property(e => e.PromptVersion).HasColumnName("promptVersion").IsRequired();
            entity.Property(e => e.TemplateVersion).HasColumnName("templateVersion").IsRequired();
            entity.Property(e => e.ProviderParametersHash).HasColumnName("providerParametersHash").IsRequired();
            entity.Property(e => e.SchemaVersion).HasColumnName("schemaVersion").IsRequired();
            entity.Property(e => e.CompletionHash).HasColumnName("completionHash").IsRequired();
            entity.Property(e => e.PromptTokens).HasColumnName("promptTokens").IsRequired();
            entity.Property(e => e.CompletionTokens).HasColumnName("completionTokens").IsRequired();
            entity.Property(e => e.DurationMs).HasColumnName("durationMs").IsRequired();
            entity.Property(e => e.RetryCount).HasColumnName("retryCount").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt").IsRequired();

            entity.HasOne(d => d.Session)
                .WithMany(p => p.ExecutionSnapshots)
                .HasForeignKey(d => d.ResearchSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CitationEntity>(entity =>
        {
            entity.ToTable("Citation");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.CanonicalReference).HasColumnName("canonicalReference").IsRequired();
            entity.Property(e => e.SourceType).HasColumnName("sourceType").IsRequired();
            entity.Property(e => e.Arabic).HasColumnName("arabic").IsRequired();
            entity.Property(e => e.Translation).HasColumnName("translation").IsRequired();
            entity.Property(e => e.Book).HasColumnName("book").IsRequired();
            entity.Property(e => e.Chapter).HasColumnName("chapter").IsRequired();
            entity.Property(e => e.Verse).HasColumnName("verse").IsRequired();
            entity.Property(e => e.HadithNumber).HasColumnName("hadithNumber").IsRequired();
            entity.Property(e => e.Edition).HasColumnName("edition").IsRequired();
            entity.Property(e => e.Url).HasColumnName("url").IsRequired();
            entity.Property(e => e.Checksum).HasColumnName("checksum").IsRequired();

            entity.HasIndex(e => e.CanonicalReference).IsUnique();
        });

        modelBuilder.Entity<BookmarkEntity>(entity =>
        {
            entity.ToTable("Bookmark");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.WorkspaceId).HasColumnName("workspaceId").IsRequired();
            entity.Property(e => e.ResearchSessionId).HasColumnName("researchSessionId").IsRequired();
            entity.Property(e => e.ReferenceId).HasColumnName("referenceId").IsRequired();
            entity.Property(e => e.Comment).HasColumnName("comment").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt").IsRequired();

            entity.HasOne(d => d.Workspace)
                .WithMany(p => p.Bookmarks)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResearchNoteEntity>(entity =>
        {
            entity.ToTable("ResearchNote");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.WorkspaceId).HasColumnName("workspaceId").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Markdown).HasColumnName("markdown").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updatedAt").IsRequired();

            entity.HasOne(d => d.Workspace)
                .WithMany(p => p.Notes)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResearchCollectionEntity>(entity =>
        {
            entity.ToTable("ResearchCollection");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Name).HasColumnName("name").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt").IsRequired();
        });

        modelBuilder.Entity<ResearchSessionCollectionEntity>(entity =>
        {
            entity.ToTable("ResearchSessionCollection");
            entity.HasKey(e => new { e.SessionId, e.CollectionId });
            entity.Property(e => e.SessionId).HasColumnName("sessionId").IsRequired();
            entity.Property(e => e.CollectionId).HasColumnName("collectionId").IsRequired();

            entity.HasOne(d => d.Session)
                .WithMany(p => p.SessionCollections)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Collection)
                .WithMany(p => p.SessionCollections)
                .HasForeignKey(d => d.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditRecordEntity>(entity =>
        {
            entity.ToTable("AuditRecord");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.Action).HasColumnName("action").IsRequired();
            entity.Property(e => e.Actor).HasColumnName("actor").IsRequired();
            entity.Property(e => e.EntityType).HasColumnName("entityType").IsRequired();
            entity.Property(e => e.EntityId).HasColumnName("entityId").IsRequired();
            entity.Property(e => e.OldStateHash).HasColumnName("oldStateHash").IsRequired();
            entity.Property(e => e.NewStateHash).HasColumnName("newStateHash").IsRequired();
            entity.Property(e => e.CorrelationId).HasColumnName("correlationId").IsRequired();
            entity.Property(e => e.RequestId).HasColumnName("requestId").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("userId").IsRequired();
            entity.Property(e => e.MachineName).HasColumnName("machineName").IsRequired();
            entity.Property(e => e.ApplicationVersion).HasColumnName("applicationVersion").IsRequired();
            entity.Property(e => e.OccurredAt).HasColumnName("occurredAt").IsRequired();

            entity.HasIndex(e => e.CorrelationId);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessage");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.EventType).HasColumnName("eventType").IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.OccurredAt).HasColumnName("occurredAt").IsRequired();
            entity.Property(e => e.ProcessedAt).HasColumnName("processedAt");
            entity.Property(e => e.Error).HasColumnName("error");

            entity.HasIndex(e => e.ProcessedAt);
        });

        modelBuilder.Entity<SearchIndexEntity>(entity =>
        {
            entity.ToTable("SearchIndex");
            entity.HasKey(e => e.EntityId);
            entity.Property(e => e.EntityId).HasColumnName("entityId").ValueGeneratedNever();
            entity.Property(e => e.EntityType).HasColumnName("entityType").IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").IsRequired();
            entity.Property(e => e.Summary).HasColumnName("summary").IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.OccurredAt).HasColumnName("occurredAt").IsRequired();
        });

        modelBuilder.Entity<MemoryEntryEntity>(entity =>
        {
            entity.ToTable("MemoryEntry");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.WorkspaceId).HasColumnName("workspaceId").IsRequired();
            entity.Property(e => e.Query).HasColumnName("query").IsRequired();
            entity.Property(e => e.Summary).HasColumnName("summary").IsRequired();
            entity.Property(e => e.ClaimsJson).HasColumnName("claimsJson").IsRequired();
            entity.Property(e => e.EvidenceIdsJson).HasColumnName("evidenceIdsJson").IsRequired();
            entity.Property(e => e.GraphNodesJson).HasColumnName("graphNodesJson").IsRequired();
            entity.Property(e => e.EvidenceHash).HasColumnName("evidenceHash").IsRequired();
            entity.Property(e => e.Methodology).HasColumnName("methodology").IsRequired();
            entity.Property(e => e.ConfidenceEvidence).HasColumnName("confidenceEvidence").IsRequired();
            entity.Property(e => e.ConfidenceCitation).HasColumnName("confidenceCitation").IsRequired();
            entity.Property(e => e.ConfidenceValidation).HasColumnName("confidenceValidation").IsRequired();
            entity.Property(e => e.ConfidenceReasoning).HasColumnName("confidenceReasoning").IsRequired();
            entity.Property(e => e.ConfidenceMethodology).HasColumnName("confidenceMethodology").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt").IsRequired();
            entity.Property(e => e.SchemaVersion).HasColumnName("schemaVersion").IsRequired();
            entity.Property(e => e.OriginSessionId).HasColumnName("originSessionId").IsRequired();
            entity.Property(e => e.OriginDocumentRevisionId).HasColumnName("originDocumentRevisionId").IsRequired();
            entity.Property(e => e.CompressedFromVersion).HasColumnName("compressedFromVersion").IsRequired();
            entity.Property(e => e.CreatedByModel).HasColumnName("createdByModel").IsRequired();
            entity.Property(e => e.PromptVersion).HasColumnName("promptVersion").IsRequired();
            entity.Property(e => e.Invalidated).HasColumnName("invalidated").IsRequired();
            entity.Property(e => e.InvalidationReason).HasColumnName("invalidationReason");

            entity.HasIndex(e => e.WorkspaceId);
        });

        modelBuilder.Entity<ResearchIterationEntity>(entity =>
        {
            entity.ToTable("ResearchIteration");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.ResearchSessionId).HasColumnName("researchSessionId").IsRequired();
            entity.Property(e => e.IterationNumber).HasColumnName("iterationNumber").IsRequired();
            entity.Property(e => e.PipelineStage).HasColumnName("pipelineStage").IsRequired();
            entity.Property(e => e.ConfidenceScore).HasColumnName("confidenceScore").IsRequired();
            entity.Property(e => e.GapsJson).HasColumnName("gapsJson").IsRequired();
            entity.Property(e => e.RetrievedNodesJson).HasColumnName("retrievedNodesJson").IsRequired();
            entity.Property(e => e.NewEvidenceJson).HasColumnName("newEvidenceJson").IsRequired();
            entity.Property(e => e.DurationMs).HasColumnName("durationMs").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt").IsRequired();

            entity.HasOne(d => d.Session)
                .WithMany(p => p.Iterations)
                .HasForeignKey(d => d.ResearchSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResearchEventEntity>(entity =>
        {
            entity.ToTable("ResearchEvent");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.ResearchSessionId).HasColumnName("researchSessionId").IsRequired();
            entity.Property(e => e.EventType).HasColumnName("eventType").IsRequired();
            entity.Property(e => e.PayloadJson).HasColumnName("payloadJson").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("createdAt").IsRequired();

            entity.HasOne(d => d.Session)
                .WithMany(p => p.Events)
                .HasForeignKey(d => d.ResearchSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ResearchResultEntity>(entity =>
        {
            entity.ToTable("ResearchResult");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.ResearchSessionId).HasColumnName("researchSessionId").IsRequired();
            entity.Property(e => e.AnswerText).HasColumnName("answerText").IsRequired();
            entity.Property(e => e.ConfidenceScore).HasColumnName("confidenceScore").IsRequired();
            entity.Property(e => e.CitationsJson).HasColumnName("citationsJson").IsRequired();
            entity.Property(e => e.Version).HasColumnName("version").IsRequired();
            entity.Property(e => e.IsFinal).HasColumnName("isFinal").IsRequired();
            entity.Property(e => e.GeneratedAt).HasColumnName("generatedAt").IsRequired();

            entity.HasOne(d => d.Session)
                .WithMany(p => p.Results)
                .HasForeignKey(d => d.ResearchSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
