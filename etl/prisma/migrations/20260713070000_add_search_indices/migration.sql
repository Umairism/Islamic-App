-- CreateIndex for Trigram search using pg_trgm
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX IF NOT EXISTS idx_verse_arabic_cleaned_trgm ON "QuranVerse" USING gin ("arabicCleaned" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_translation_text_trgm ON "QuranTranslation" USING gin ("text" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_surah_english_name_trgm ON "Surah" USING gin ("englishName" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_surah_transliteration_trgm ON "Surah" USING gin ("transliteration" gin_trgm_ops);
