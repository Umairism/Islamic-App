# Islamic Research Platform (Project Specification)

## Vision

Build a professional Islamic research platform comparable in workflow to legal research systems (e.g. legal databases), where users ask questions or search topics and receive organized evidence from the Qur'an, authentic Hadith, and classical Tafsir.

The application is **not** an AI Mufti. It is an evidence-first research assistant.

---

# Primary Goals

- Fast and accurate research.
- Every answer backed by citations.
- Separate revelation, scholarly interpretation, and AI-generated summaries.
- Support students, teachers, researchers, imams, and general users.
- Scalable architecture.

---

# Core Principles

1. Qur'an is the highest authority.
2. Hadith are stored with complete metadata (book, chapter, narrator, grading where applicable).
3. Tafsir is clearly attributed.
4. AI never invents evidence.
5. Every generated answer lists the exact sources used.

---

# User Types

- Guest
- Registered User
- Student of Knowledge
- Researcher
- Administrator

---

# Functional Modules

## 1. Search Engine

Support:
- Keyword search
- Phrase search
- Arabic search
- Urdu search
- English search
- Surah/Ayah search
- Hadith number search
- Topic search

Filters:
- Qur'an
- Hadith collections
- Tafsir
- Language
- Date added
- Scholar

---

## 2. Question Mode

Example:

> What are the rights of parents?

Pipeline:

1. Parse question.
2. Detect concepts.
3. Search indexed sources.
4. Rank results.
5. Build evidence dossier.
6. Generate AI summary from retrieved evidence only.
7. Show citations.

---

## 3. Evidence Dossier

Contains:

- Question
- Related Topics
- Qur'anic Verses
- Hadith
- Tafsir
- Cross References
- Scholarly Opinions (future)
- AI Summary
- References

---

## 4. Knowledge Graph

Entities:
- Topics
- Verses
- Hadith
- Tafsir
- Scholars
- Narrators

Relationships:
- explains
- references
- similar_to
- about
- narrated_by

---

## 5. Personal Workspace

- Bookmarks
- Notes
- Reading history
- Collections
- Export

---

## 6. Citation System

Each answer should include:
- Source
- Book
- Chapter
- Verse/Hadith Number
- Translation used

---

# Data Sources

Preferred:
- XML
- JSON
- SQL dumps

Avoid PDFs unless no structured source exists.

---

# Database Design (Conceptual)

Tables/Collections

## QuranVerse

- id
- surah
- ayah
- arabic
- translations
- keywords
- topics

## Hadith

- id
- collection
- book
- chapter
- number
- arabic
- translation
- narrator
- grading
- keywords
- topics

## Tafsir

- id
- scholar
- verse_reference
- text
- topics

## Topic

- id
- title
- aliases
- description

---

# Search Pipeline

User Query

↓

Normalization

↓

Language Detection

↓

Concept Extraction

↓

Synonym Expansion

↓

Full-text Search

↓

Knowledge Graph Expansion

↓

Ranking

↓

Evidence Builder

↓

AI Summary

↓

Response

---

# AI Rules

Allowed:
- Summarize
- Organize
- Explain
- Compare
- Link references

Forbidden:
- Invent evidence
- Hide disagreements
- Omit citations
- Present opinion as revelation

---

# Future Features

- Arabic morphology
- Root-word search
- Voice search
- OCR import
- Offline desktop edition
- Mobile apps
- Multi-language UI
- Scholar comparison
- Timeline views
- Family trees of narrators
- API for developers

---

# Suggested Tech Stack

Frontend:
- React
- TypeScript

Backend:
- ASP.NET Core Web API

Database:
- PostgreSQL

Search:
- PostgreSQL Full Text Search
- Elasticsearch/Meilisearch later

Desktop:
- Tauri

---

# Development Roadmap

Phase 1
- Qur'an
- Search
- Bookmarks

Phase 2
- Sahih al-Bukhari
- Sahih Muslim

Phase 3
- Cross references

Phase 4
- Tafsir

Phase 5
- Semantic search

Phase 6
- Knowledge graph

Phase 7
- AI evidence summarization

---

# Long-term Vision

Become a trusted Islamic research platform where every answer is transparent, evidence-based, reproducible, and easy to verify.
