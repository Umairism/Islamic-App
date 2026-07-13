# ADR 0003: Clean Architecture & System Layers

* **Status**: Approved
* **Date**: 2026-07-10
* **Author**: Principal Software Architect

## Context

To prevent a complex application with millions of relationships from deteriorating into a monolithic dependency mess, we must enforce strict architectural separation. We need to prevent framework details (like Next.js/React, ASP.NET Core) or database logic (like Prisma, raw SQL) from leaking into core domain entities and business rules.

## Decision

We will strictly adhere to **Clean Architecture** principles. The codebase is partitioned into distinct layers with unidirectional dependency rules:

1. **Domain Layer**: Contains pure business entities, value objects, domain interfaces, and core domain rules. No dependencies on databases, web frameworks, or third-party ORMs are permitted.
2. **Application Layer**: Contains application interfaces, use cases, search request contracts, and pipeline schemas. It coordinates operations but remains independent of database/API delivery frameworks.
3. **Infrastructure Layer**: Implements database engines (Prisma, Entity Framework), search engines (PostgreSQL FTS, vector search), HTTP clients, and configuration engines.
4. **Presentation/UI Layer**: Delivery frameworks (Next.js client interface, Web API controllers) that present data to the user.

### Dependency Rule

Dependencies must only point *inward*:
* **UI/Controllers** ➔ **Application** ➔ **Domain**
* **Infrastructure** ➔ **Application** ➔ **Domain**

Any code violating these boundaries (e.g. database logic in React views or business validation inside controllers) is strictly forbidden.

## Consequences

* **Pros**:
  * Infrastructure elements (database, search engine, indexing engine) are completely swappable without touching core logic.
  * Clear separation of responsibilities makes code easy to test and navigate.
* **Cons**:
  * Requires explicit DTO-to-Domain mappings, introducing minor boilerplate code.
