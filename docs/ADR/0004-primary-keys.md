# ADR 0004: Primary Keys Selection: CUID vs. Integer Sequences

* **Status**: Approved
* **Date**: 2026-07-10
* **Author**: Principal Database Architect

## Context

Using simple sequential integers (such as global verse sequence numbers 1-6236 or Hadith indices 1-7277) as database Primary Keys (PKs) creates severe long-term coupling:
1. **Edition Divergence**: If the platform later imports different text editions (e.g. Tanzil vs. Uthmani, different Hadith numbering editions), the verse sequences might not map 1:1, leading to collisions.
2. **Referential Integrity**: Truncating or recreating tables during ETL pipelines invalidates all user-workspace foreign keys (bookmarks, highlights, notes) and search indexes pointing to those numeric IDs.
3. **Distributed Generation**: Numeric auto-increment PKs require database round-trips, preventing client-side or importer-side ID generation.

## Decision

We will decouple database physical identity from business/textual identity:

1. **Database Primary Keys**: All tables will use CUID (Collision-resistant Unique Identifier) strings as their primary keys (e.g., `id String @id @default(cuid())`).
2. **Business Sequence Identifiers**:
   * For the Qur'an, `globalIndex` (Int, 1 to 6236) will be mapped as a unique secondary integer column.
   * For Hadiths, `hadithId` (Int, the sequence index within the collection) will be stored.
3. **Composite Unique Business Keys**:
   * For Quranic verses, we enforce a unique constraint on `(surahNumber, ayahNumber)`.
   * For Hadiths, we enforce a unique constraint on `(collectionId, hadithId)`.

## Consequences

* **Pros**:
  * Users can bookmark a verse (`id`), and that bookmark remains valid even if we re-import the text, change the translation, or alter the `globalIndex` mapping.
  * Allows stable reference integrity between different Quran editions.
* **Cons**:
  * Slightly larger storage size on disk and indices due to string PKs instead of 4-byte integers.
