# Research Engine & Lexical Search System

The Islamic Research Platform utilizes a decoupled, deterministic, and explainable lexical search system to match, rank, and highlight relevant primary source texts (currently Quranic verses).

---

## 1. Pipeline Architecture

The search process is structured as a pipeline of independent execution stages modifying a shared `SearchContext`:

```text
Request
  │
  ▼
QueryClassifier (Identifies query category: Reference, Keyword, Arabic, Translation, Mixed)
  │
  ▼
ISearchNormalizer (Normalizes spacing, spelling, casing, tatweel, and Arabic diacritics)
  │
  ▼
ITokenizer (Splits query text into words and removes stopwords from stopwords.json)
  │
  ▼
ISynonymEngine (Expands query words using weighted mapping entries in synonyms.json)
  │
  ▼
ISourceReferenceResolver (Resolves reference patterns and maps Ayat al-Kursi aliases in aliases.json)
  │
  ▼
DatabaseQueryStage (Queries trigram PostgreSQL databases on indexed columns)
  │
  ▼
IRankingEngine (Assigns scores to candidates using configurable settings)
  │
  ▼
IHighlightBuilder (Snippetizes matching translation contexts with HTML <em> tags)
  │
  ▼
IEvidenceBuilder (Maps generic presentation-ready EvidenceItem results)
  │
  ▼
EvidenceDossier Output
```

---

## 2. In-Memory Autocomplete Suggestions

To ensure fast suggest responses, the autocomplete suggestion index `SuggestionIndex` is built on startup. Surah English/Transliterated names, custom aliases, and popular references are cached in memory in a prefix list, returning matches instantly without query overhead.

---

## 3. Database Indexes Optimization

We optimize database query scans on PostgreSQL using `pg_trgm` GIN indexes applied to clean Arabic text, translation text, and Surah names:

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX IF NOT EXISTS idx_verse_arabic_cleaned_trgm ON "QuranVerse" USING gin ("arabicCleaned" gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_translation_text_trgm ON "QuranTranslation" USING gin ("text" gin_trgm_ops);
```

---

## 4. Deterministic Ranking Logic

Candidates are scored using deterministic, config-driven weights from `ranking.json`:

* **Exact Reference**: 100 points
* **Alias Reference**: 95 points
* **Exact Arabic Match**: 90 points
* **Exact Translation Match**: 80 points
* **Surah Name Match**: 75 points
* **Synonym Match**: 65 points (multiplied by token synonym weight)
* **Partial Term Match**: 40 points

Every matching result records a detailed `MatchReason` trace and score details to support auditing search results.

---

## 5. Future Semantic Search Extensions

The architecture is built with extensibility in mind:
- **Hadith & Tafsir Ingestion**: The generic `EvidenceItem` is fully decoupling fields (e.g. `SourceType`, `SourceName`, `PrimaryText`, `Translations`) to plug new sources directly into the search pipeline.
- **Hybrid/Semantic Search**: An additional `SemanticSearchStage` can be added to the pipeline to fetch vector similarity matches from an external embedding database and merge candidates with lexical search scores before ranking.
