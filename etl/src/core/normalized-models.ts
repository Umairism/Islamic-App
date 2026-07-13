/**
 * Domain Models for the Islamic Research Platform (ETL Context)
 * 
 * These interfaces represent the clean, normalized domain objects used inside the
 * ingestion pipeline. They decouple the data importers and database sinks from raw 
 * JSON schema details (DTOs) of different translation/source files.
 */

export interface NormalizedSurah {
  number: number;
  arabicName: string;
  transliteration: string;
  englishName: string;
  revelationType: string;
  totalVerses: number;
}

export interface NormalizedVerse {
  globalIndex: number;
  surahNumber: number;
  ayahNumber: number;
  arabicText: string;
  arabicCleaned: string;
  transliteration: string;
}

export interface NormalizedTranslation {
  surahNumber: number;
  ayahNumber: number;
  language: string;
  translator: string;
  text: string;
}

export interface NormalizedQuranData {
  surahs: NormalizedSurah[];
  verses: NormalizedVerse[];
  translations: NormalizedTranslation[];
}

/**
 * Domain Model representing a standardized document ready for search indexing.
 * This ensures PostgreSQL, Meilisearch, and Elasticsearch sinks share a common format.
 */
export interface SearchDocument {
  id: string;
  text: string;
  cleanedText: string;
  language: string;
  title: string;
  type: 'QURAN' | 'HADITH' | 'TAFSIR';
  keywords: string[];
}
