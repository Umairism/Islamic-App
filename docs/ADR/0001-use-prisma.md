# ADR 0001: Use Prisma for Database Schema & Migrations

* **Status**: Approved
* **Date**: 2026-07-10
* **Author**: Principal Software Architect

## Context

Managing database schemas via raw SQL migrations becomes increasingly error-prone and painful to maintain as the project grows (adding Quran editions, multiple Hadith collections, Tafsir commentaries, semantic concepts, and user workspace collections).

We need an object-relational mapping (ORM) and schema management layer that:
1. Provides structured, declarative schema modeling.
2. Supports automated, repeatable, and reversible migrations.
3. Generates typed database client interfaces for ETL ingestion and backend logic, preventing SQL mapping errors.

## Decision

We will use **Prisma** as the database schema manager and database client generator for both the ETL pipeline (Node.js) and other JS-based layers. 

* The database schema is defined in a single source of truth: `etl/prisma/schema.prisma`.
* Physical database changes must be generated via `npx prisma migrate dev` during local development, producing incremental, versioned migration directories.
* In production/staging, migrations are applied using `npx prisma migrate deploy`. Direct manual schema mutations or `prisma db push` commands are forbidden in production.

## Consequences

* **Pros**:
  * Declarative schema representation, making audits and relationship mapping highly readable.
  * Auto-generated type-safe queries for ETL pipelines.
  * Standardized migration histories.
* **Cons**:
  * Introduces compile-time dependencies on Prisma Client generation.
  * Adds dependency footprint to the ETL folder.
