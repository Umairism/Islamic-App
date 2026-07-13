# ADR 0002: Use PostgreSQL with pgvector as Primary Database

* **Status**: Approved
* **Date**: 2026-07-10
* **Author**: Principal Database Architect

## Context

The Islamic Research Platform deals with highly structured, interconnected textual data:
1. **Quranic Verses**: Have exact chapter/verse relations, multi-language translations, and scholarly commentary (Tafsir).
2. **Hadith Narrations**: Have books, chapters, narrator chains, and cross-references.
3. **Knowledge Graph**: Interlinks topics/concepts with primary texts.

We require a database engine that handles relational integrity, full-text searches, and vector embeddings (for future semantic search) inside a single ecosystem.

## Decision

We will use **PostgreSQL** as the primary relational database system, utilizing the **`pgvector`** extension for vector similarity index storage. 

* The schema is designed for relational consistency (foreign keys and cascades).
* Lexical text searches will utilize PostgreSQL Full-Text Search (FTS) indexes (`tsvector` and `tsquery`).
* Semantic vector searches will store 1536-dimensional embeddings and query them using cosine distance operators.

## Consequences

* **Pros**:
  * Highly stable relational database engine with mature tooling.
  * Native integration of vector indexing via `pgvector`, removing the need for a separate vector database in initial phases.
  * Built-in text search capabilities.
* **Cons**:
  * PostgreSQL requires manual configuration of `pgvector` when not running in Docker (e.g. native Windows environments require compiling the extension).
