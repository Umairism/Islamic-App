-- DropIndex
DROP INDEX "idx_translation_text_trgm";

-- DropIndex
DROP INDEX "idx_verse_arabic_cleaned_trgm";

-- DropIndex
DROP INDEX "idx_surah_english_name_trgm";

-- DropIndex
DROP INDEX "idx_surah_transliteration_trgm";

-- CreateTable
CREATE TABLE "HadithCollection" (
    "id" TEXT NOT NULL,
    "slug" TEXT NOT NULL,
    "shortName" TEXT NOT NULL,
    "displayName" TEXT NOT NULL,
    "titleArabic" TEXT NOT NULL,
    "titleEnglish" TEXT NOT NULL,
    "authorArabic" TEXT NOT NULL,
    "authorEnglish" TEXT NOT NULL,
    "introductionArabic" TEXT NOT NULL,
    "introductionEnglish" TEXT NOT NULL,
    "totalHadiths" INTEGER NOT NULL,

    CONSTRAINT "HadithCollection_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "HadithBook" (
    "id" TEXT NOT NULL,
    "collectionId" TEXT NOT NULL,
    "bookNumber" INTEGER NOT NULL,
    "titleArabic" TEXT NOT NULL,
    "titleEnglish" TEXT NOT NULL,

    CONSTRAINT "HadithBook_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "HadithChapter" (
    "id" TEXT NOT NULL,
    "bookId" TEXT NOT NULL,
    "chapterNumber" INTEGER NOT NULL,
    "titleArabic" TEXT NOT NULL,
    "titleEnglish" TEXT NOT NULL,

    CONSTRAINT "HadithChapter_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "Hadith" (
    "id" TEXT NOT NULL,
    "collectionId" TEXT NOT NULL,
    "bookId" TEXT NOT NULL,
    "chapterId" TEXT NOT NULL,
    "hadithNumber" INTEGER NOT NULL,
    "canonicalNumber" TEXT,
    "originalNumber" TEXT,
    "arabicText" TEXT NOT NULL,
    "arabicCleaned" TEXT NOT NULL,
    "englishNarrator" TEXT NOT NULL,
    "englishText" TEXT NOT NULL,

    CONSTRAINT "Hadith_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "HadithCollection_slug_key" ON "HadithCollection"("slug");

-- CreateIndex
CREATE INDEX "Hadith_collectionId_hadithNumber_idx" ON "Hadith"("collectionId", "hadithNumber");

-- AddForeignKey
ALTER TABLE "HadithBook" ADD CONSTRAINT "HadithBook_collectionId_fkey" FOREIGN KEY ("collectionId") REFERENCES "HadithCollection"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "HadithChapter" ADD CONSTRAINT "HadithChapter_bookId_fkey" FOREIGN KEY ("bookId") REFERENCES "HadithBook"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "Hadith" ADD CONSTRAINT "Hadith_collectionId_fkey" FOREIGN KEY ("collectionId") REFERENCES "HadithCollection"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "Hadith" ADD CONSTRAINT "Hadith_bookId_fkey" FOREIGN KEY ("bookId") REFERENCES "HadithBook"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "Hadith" ADD CONSTRAINT "Hadith_chapterId_fkey" FOREIGN KEY ("chapterId") REFERENCES "HadithChapter"("id") ON DELETE CASCADE ON UPDATE CASCADE;

-- Re-create extension and GIN trigram indexes
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX IF NOT EXISTS "idx_verse_arabic_cleaned_trgm" ON "QuranVerse" USING gin ("arabicCleaned" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS "idx_translation_text_trgm" ON "QuranTranslation" USING gin ("text" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS "idx_surah_english_name_trgm" ON "Surah" USING gin ("englishName" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS "idx_surah_transliteration_trgm" ON "Surah" USING gin ("transliteration" gin_trgm_ops);

CREATE INDEX IF NOT EXISTS "idx_hadith_arabic_cleaned_trgm" ON "Hadith" USING gin ("arabicCleaned" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS "idx_hadith_english_text_trgm" ON "Hadith" USING gin ("englishText" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS "idx_hadith_english_narrator_trgm" ON "Hadith" USING gin ("englishNarrator" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS "idx_hadith_book_title_trgm" ON "HadithBook" USING gin ("titleEnglish" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS "idx_hadith_chapter_title_trgm" ON "HadithChapter" USING gin ("titleEnglish" gin_trgm_ops);
