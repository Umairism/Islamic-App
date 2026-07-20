/**
 * Core domain types for Islamic Research Platform
 * Follows clean architecture principles - domain types are independent of UI/API
 */

// ============================================================================
// Enums & Constants
// ============================================================================

export enum SourceType {
  QURAN = 'quran',
  HADITH = 'hadith',
  TAFSIR = 'tafsir',
  FIQH = 'fiqh',
  BIOGRAPHY = 'biography',
  HISTORY = 'history',
}

export enum HadithAuthenticity {
  SAHIH = 'sahih',
  HASAN = 'hasan',
  DAI_IF = 'daiif',
  MAUDU = 'maudu',
  UNKNOWN = 'unknown',
}

export enum LanguageCode {
  EN = 'en',
  AR = 'ar',
  UR = 'ur',
}

export enum SortOption {
  RELEVANCE = 'relevance',
  DATE_DESC = 'date_desc',
  DATE_ASC = 'date_asc',
  TITLE_ASC = 'title_asc',
  TITLE_DESC = 'title_desc',
}

// ============================================================================
// Document & Source Models
// ============================================================================

/**
 * Base document metadata shared across all source types
 */
export interface DocumentMetadata {
  id: string;
  sourceType: SourceType;
  title: string;
  titleArabic?: string;
  description?: string;
  descriptionArabic?: string;
  language: LanguageCode;
  createdDate?: Date;
  author?: string;
  authorArabic?: string;
  tags: string[];
  citations: number;
  relevanceScore?: number;
}

/**
 * Quranic verse document
 */
export interface QuranVerse extends DocumentMetadata {
  sourceType: SourceType.QURAN;
  surahNumber: number;
  surahName: string;
  surahNameArabic: string;
  ayahNumber: number;
  text: string;
  textArabic: string;
  transliteration?: string;
  translations: Translation[];
}

/**
 * Hadith document
 */
export interface Hadith extends DocumentMetadata {
  sourceType: SourceType.HADITH;
  text: string;
  textArabic: string;
  narrator: string;
  narratorArabic?: string;
  collection: string;
  bookNumber?: number;
  hadithNumber?: number;
  authenticity: HadithAuthenticity;
  chains: string[];
  grades: HadithGrade[];
}

/**
 * Tafsir commentary
 */
export interface Tafsir extends DocumentMetadata {
  sourceType: SourceType.TAFSIR;
  quranReference: {
    surahNumber: number;
    surahName: string;
    ayahStart: number;
    ayahEnd: number;
  };
  text: string;
  textArabic: string;
  scholar: string;
  scholarArabic?: string;
  tafsirSchool?: string;
  relatedAyahs: string[];
}

/**
 * Fiqh (Islamic jurisprudence) document
 */
export interface Fiqh extends DocumentMetadata {
  sourceType: SourceType.FIQH;
  text: string;
  textArabic: string;
  school: string; // Hanafi, Maliki, Shafi'i, Hanbali
  topic: string;
  topicArabic?: string;
  rulings: string[];
  relatedSources: string[];
}

/**
 * Union type for all documents
 */
export type Document = QuranVerse | Hadith | Tafsir | Fiqh;

/**
 * Translation of text (Quran or Hadith)
 */
export interface Translation {
  language: LanguageCode;
  translator: string;
  text: string;
}

/**
 * Hadith grade from various scholars
 */
export interface HadithGrade {
  scholar: string;
  authenticity: HadithAuthenticity;
  notes?: string;
}

// ============================================================================
// Search & Query Models
// ============================================================================

/**
 * Search query parameters
 */
export interface SearchQuery {
  term: string;
  sourceTypes?: SourceType[];
  language?: LanguageCode;
  sort: SortOption;
  page: number;
  pageSize: number;
  filters?: SearchFilters;
}

/**
 * Advanced search filters
 */
export interface SearchFilters {
  dateRange?: {
    from: Date;
    to: Date;
  };
  authenticityFilter?: HadithAuthenticity[];
  schoolFilter?: string[]; // For Fiqh
  tagsFilter?: string[];
  authorFilter?: string[];
  citationMin?: number;
  citationMax?: number;
}

/**
 * Search results response
 */
export interface SearchResults {
  query: SearchQuery;
  total: number;
  documents: Document[];
  facets: SearchFacets;
  executionTime: number;
}

/**
 * Search facets for filtering
 */
export interface SearchFacets {
  sourceTypes: { type: SourceType; count: number }[];
  authenticities: { authenticity: HadithAuthenticity; count: number }[];
  schools: { school: string; count: number }[];
  authors: { author: string; count: number }[];
  tags: { tag: string; count: number }[];
}

// ============================================================================
// Collection & Saved Items
// ============================================================================

/**
 * User collection (for saved/favorite documents)
 */
export interface Collection {
  id: string;
  name: string;
  description?: string;
  documentIds: string[];
  createdAt: Date;
  updatedAt: Date;
  isPublic: boolean;
  tags: string[];
}

/**
 * Saved search for recurring queries
 */
export interface SavedSearch {
  id: string;
  name: string;
  query: SearchQuery;
  createdAt: Date;
  lastRunAt?: Date;
  note?: string;
}

/**
 * Annotation on a document
 */
export interface Annotation {
  id: string;
  documentId: string;
  text: string;
  highlightedText: string;
  selectionStart: number;
  selectionEnd: number;
  type: 'note' | 'highlight' | 'bookmark';
  color?: string;
  createdAt: Date;
  updatedAt: Date;
}

// ============================================================================
// User Preferences & Settings
// ============================================================================

/**
 * User preferences
 */
export interface UserPreferences {
  language: LanguageCode;
  theme: 'light' | 'dark' | 'system';
  itemsPerPage: number;
  defaultSortOption: SortOption;
  textSize: 'small' | 'normal' | 'large' | 'xlarge';
  highlightedSources: SourceType[];
  autoSave: boolean;
  rtlEnabled: boolean;
}

// ============================================================================
// Pagination & Sorting
// ============================================================================

/**
 * Pagination info
 */
export interface PaginationInfo {
  currentPage: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPrevPage: boolean;
}

/**
 * Generic paginated response
 */
export interface PaginatedResponse<T> {
  data: T[];
  pagination: PaginationInfo;
}

// ============================================================================
// API Response Models
// ============================================================================

/**
 * Standard API response wrapper
 */
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: {
    code: string;
    message: string;
    details?: unknown;
  };
  metadata?: {
    timestamp: string;
    version: string;
  };
}

/**
 * Search-specific API response
 */
export interface SearchApiResponse extends ApiResponse<SearchResults> {
  cacheHit?: boolean;
}

// ============================================================================
// Research Session & Streaming Lifecycle Models (Milestone 10)
// ============================================================================

export enum ResearchSessionStatus {
  Created = 'Created',
  Queued = 'Queued',
  Running = 'Running',
  WaitingForIteration = 'WaitingForIteration',
  Completed = 'Completed',
  Failed = 'Failed',
  Cancelled = 'Cancelled'
}

export enum PipelineStage {
  Retrieval = 'Retrieval',
  Deduplication = 'Deduplication',
  Analysis = 'Analysis',
  Reasoning = 'Reasoning',
  Validation = 'Validation',
  Explainability = 'Explainability',
  Rendering = 'Rendering',
  Completed = 'Completed',
  Failed = 'Failed'
}

export interface ResearchEventSignalR {
  sessionId: string;
  eventType: string;
  stage?: PipelineStage;
  timestamp: string;
  payloadJson?: string;
}

export interface ResearchClaim {
  statement: string;
  confidence: number;
  supportingEvidence: string[];
  claimType: string;
  origin: string;
}

export interface ResearchResultDto {
  sessionId: string;
  version: number;
  answerText: string;
  confidenceScore: number;
  isFinal: boolean;
  claims: ResearchClaim[];
  renderedMarkdown?: string;
  renderedHtml?: string;
}

export interface MemoryEntryDto {
  id: string;
  workspaceId: string;
  query: string;
  summary: string;
  confidenceOverall: number;
  evidenceCount: number;
  createdAt: string;
}
