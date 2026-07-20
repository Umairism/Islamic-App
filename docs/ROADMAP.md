# Project Roadmap: Islamic Research Platform

This roadmap tracks the development progress of the Islamic Research Platform, from base database ETL ingestion up to agentic iterative reasoning loops.

---

## Complete Feature Roadmap

### Milestone 1 — Ingestion & Storage (Completed)
- [x] Docker database containerization (PostgreSQL + `pgvector` extension)
- [x] Ingest Quran raw texts and 10 translations with full schema mapping
- [x] Establish ETL Pipeline Engine validation reports and CLI ingestion scripts

### Milestone 2 — Read-Only Research API (Completed)
- [x] Set up ASP.NET Core Clean Architecture backend structure
- [x] Define entity mappings, repositories, and Specification query patterns
- [x] Expose REST endpoints for verse lookups, health metrics, and API metadata
- [x] Setup global exception handling and Swagger documentation

### Milestone 3 — Hybrid Search Engine (Completed)
- [x] Implement lexical search indexing using PostgreSQL full-text search
- [x] Implement semantic dense vector searches using pgvector and embeddings
- [x] Build hybrid search ranker merging lexical and semantic results

### Milestone 4 — Hadith Ingestion (Completed)
- [x] Ingest Sahih al-Bukhari & Sahih Muslim raw datasets
- [x] Extended ETL logic to extract narrator chains, gradings, and chapters
- [x] Setup Hadith query schemas and DB seed scripts

### Milestone 5 — Classical Tafsir Ingestion (Completed)
- [x] Ingest classical Tafsir texts (e.g. Ibn Kathir)
- [x] Map Tafsir paragraphs directly to Qur'an verses

### Milestone 6 — Semantic Knowledge Graph (Completed)
- [x] Build Knowledge Graph model linking Verses, Hadiths, Topics, and Narrators
- [x] Add semantic relations: `explains`, `narrated_by`, `refers_to`
- [x] Support hybrid graph expansions in search matching

### Milestone 7 — AI Reasoning & Validation (Completed)
- [x] Setup local LLM integrations and PromptBuilders
- [x] Implement strict validation rules (Claim validation, Citation validation)
- [x] Generate cited, verifiable research dossiers with explainable metadata

### Milestone 8 — Workspace Management & Outbox (Completed)
- [x] Add personal workspace bookmarks, notes, and collections
- [x] Implement transactional Outbox pattern background workers
- [x] Create export writer formats (Markdown, HTML, PDF, JSON, DOCX)

### Milestone 9 — Knowledge Memory & Agentic loops (Completed)
- [x] Build time-decayed append-only workspace memories
- [x] Implement non-recursive `while` execution loops in pipeline stage transitions
- [x] Add pluggable `IIterationPlanner` resolving gaps and emitting `RetrievalPlan` instances
- [x] Configure explainable composite confidence calculators

### Milestone 10 — Research Execution Platform & UI Integration (Completed)
- [x] Persistent Research Session domain model & audit event store (`ResearchSession`, `ResearchIteration`, `ResearchEvent`, `ResearchResult`)
- [x] Channel-based async background execution worker (`ResearchBackgroundWorker`)
- [x] Realtime stage progress streaming via WebSockets/SignalR (`ResearchHub`)
- [x] Optimistic concurrency protection with concurrency tokens
- [x] Next.js 16 / React 19 legal-grade research workspace UI (`frontend/components/research/`)
- [x] Live PostgreSQL database validation and pipeline execution integration tests
