# Project Roadmap: Islamic Research Platform

This roadmap serves as the long-term project compass, guiding development phases from primary ingestion up to evidence ranking and AI RAG pipelines.

---

## Roadmap

### v0.1 — Ingestion & Storage
* [x] Docker Container Infrastructure (PostgreSQL + `pgvector`)
* [x] Complete ETL Pipeline Engine (`BaseImporter`, validation, normalization, reporting)
* [x] Import Qur'an raw text and 10 translations
* [x] Apply schema migrations via Prisma
* [x] Verify database constraints and cleaned Arabic search index
* [x] Git Tag `v0.1-etl-complete`

### v0.2 — Read-Only Research API (Current)
* [x] Install .NET 8 SDK
* [ ] Establish Clean Architecture backend solution
* [ ] Define repositories (`ISurahRepository`, `IVerseRepository`, `ITranslationRepository`) and specification pattern
* [ ] Build `EvidenceService` wrapping core Quranic research retrieval
* [ ] Implement versioned REST endpoints (`/api/v1/quran/...` and `/api/v1/system/...`)
* [ ] Expose `GET /health` with comprehensive metrics and `GET /api/version`
* [ ] Add Correlation ID middleware (`X-Correlation-ID` headers & logs)
* [ ] Setup global error middleware (`RFC 7807` standard)
* [ ] Setup Swagger/OpenAPI with XML code documentation
* [ ] Verify endpoints with basic unit and integration tests

### v0.3 — Hybrid Search Engine
* [ ] Implement Clean Architecture Search layer
* [ ] Define lexical search (Full-Text Search) for Arabic and translation text
* [ ] Setup dense vector embedding generator for semantic search using pgvector
* [ ] Build search matching logic (hybrid lexical + semantic ranking)

### v0.4 — Hadith Ingestion
* [ ] Ingest Sahih al-Bukhari & Sahih Muslim raw datasets
* [ ] Extend ETL pipeline to parse narrator chains, gradings, and chapters
* [ ] Perform database seeding and schema expansions

### v0.5 — Classical Tafsir Ingestion
* [ ] Ingest classical Tafsir texts (e.g. Ibn Kathir)
* [ ] Map Tafsir paragraphs relative to Qur'an verses

### v0.6 — Semantic Knowledge Graph & Cross-References
* [ ] Build Knowledge Graph architecture mapping entities (`Topic`, `Narrator`, `Scholar`)
* [ ] Define semantic relation tags (`explains`, `narrated_by`, `refers_to`)
* [ ] Establish cross-referencing between Quran verses and authentic Hadith

### v0.7 — Evidence dossier Builder
* [ ] Implement query conceptual parsing
* [ ] Build `Evidence dossier Builder` collecting references, topics, narrators, and scholar explanations
* [ ] Group evidence into dossiers

### v0.8 — AI RAG Synthesis
* [ ] Setup local/cloud LLM client integrations
* [ ] Implement strict citation enforcement rules (Evidence-First summarization)
* [ ] Produce dossiers with side-by-side verification citations

### v0.9 — User Collections & Personal Research Workspace
* [ ] Implement authentication (optional/local)
* [ ] Add Bookmarks, Collections, Notes, and custom dossiers

### v1.0 — Public Launch
* [ ] Production deployments, caching, and search indices optimization
* [ ] Complete next-generation Frontend client dashboard UI
