# ADR 0005: Search Strategy & Decoupling

* **Status**: Approved
* **Date**: 2026-07-10
* **Author**: Principal Search Engineer

## Context

Search quality is the highest priority for the Islamic Research Platform. It must support exact keyword matching, semantic meaning search, topic expansion (via the Knowledge Graph), and multi-lingual queries.

However, tightly coupling search index logic directly to the primary relational database causes two major problems:
1. **Performance Bottlenecks**: Heavy search queries degrade transaction performance.
2. **Infrastructure Coupling**: If we later migrate from PostgreSQL FTS/Vector to dedicated search engines (like Meilisearch, Elasticsearch) or vector databases (like Qdrant, Milvus), we would have to rewrite large parts of the application.

## Decision

1. **Decoupled Search Indexes**: The database serves as the single source of truth for transaction persistence. Search query processing is handled through decoupled **Search Sinks** and **Search Client** interfaces.
2. **Standardized Search Documents**:
   * The application layer defines a `SearchDocument` model containing fields optimized for indexing: `id`, `text`, `cleanedText`, `language`, `title`, `type` (Quran/Hadith/Tafsir), and `keywords`.
   * Sinks map database entities (like `QuranVerse`) to `SearchDocument` values before publishing.
3. **Hybrid Search Pipeline**:
   * **Stage 1 (Lexical FTS)**: Keyword matching using language-specific stop words and diacritics removal.
   * **Stage 2 (Semantic Embeddings)**: Distance queries using dense vector representation (postponed until later milestones).
   * **Stage 3 (RRF - Reciprocal Rank Fusion)**: Ranks from both FTS and vector searches are merged.
4. **Postponed Vector Embeddings**: We will not generate dense vector embeddings in Milestone 1. We will establish exact keyword and semantic mapping queries first, as generating embeddings prematurely without validating exact searches leads to performance bottlenecks and unnecessary cost.

## Consequences

* **Pros**:
  * We can swap PostgreSQL search for Elasticsearch or Meilisearch without altering any domain or business rules.
  * Allows scaling the search nodes independently from the database nodes.
* **Cons**:
  * Requires maintaining a search indexing publisher pipeline, which syncs updates from PostgreSQL to the search engine.
