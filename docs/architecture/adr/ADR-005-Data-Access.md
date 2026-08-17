# ADR-005: Data Access

## Status

Accepted

Accepted by Product Owner and CTO on 2026-08-17 (Sprint 0.3A Architecture Freeze).

---

## Context

HuGuWeb will persist transactional hotel data (reservations, rooms, stays, folio, identity, and later modules as approved). The backend is .NET 10 (ADR-002) with PostgreSQL (ADR-004) in a modular monolith (ADR-001).

The false choice “EF Core **or** SQL forever” is rejected. Productivity for writes and consistency matters now. Specialized SQL for heavy reporting may matter later.

---

## Problem

How should HuGuWeb access the database at bootstrap, and when (if ever) should raw SQL or micro-ORMs be introduced?

---

## Decision

We will use **Entity Framework Core 10** as the primary data-access approach for transactional domain persistence.

Strategy:

- **EF Core for transactional persistence**
- **Raw SQL where justified** (EF Core `FromSql` / raw SQL as the first escape hatch)
- **Dapper is not added at bootstrap**
- Dapper may be introduced later for measured reporting/read performance needs

Recommended major version: **EF Core 10** (LTS, aligned with .NET 10).

Verified from Microsoft docs (as of 2026-08-17):

- EF Core 10.0 released November 2025
- LTS; supported until **10 November 2028**
- Requires .NET 10

Do not pin patch versions in this ADR. Use the latest 10.0.x at implementation time.

**Pragmatic later strategy (not bootstrap):**

- EF Core for transactional writes, change tracking, and module persistence
- EF Core `FromSql` / raw SQL as the first escape hatch
- Dapper (or equivalent) **only** when a measured reporting/read-heavy query is painful in EF Core

**Do not introduce Dapper now.** There is no reporting workload, no performance evidence, and no schema yet.

---

## Alternatives Considered

| Alternative | Outcome | Reason |
|-------------|---------|--------|
| EF Core 10 as primary | Accepted | Migrations, change tracking, transactions, testability, and team productivity. Fits modular DbContexts. |
| Dapper (or ADO.NET) as primary | Rejected | Too much manual mapping for an ERP that will evolve. Weakens change-safety during early schema flux. |
| EF Core only, forever, no SQL | Rejected as dogma | Complex reporting and cross-module reads may need SQL later. Keep the door open. |
| Dapper alongside EF Core at bootstrap | Rejected | Two data stacks before one exists. Premature. |

### Considerations

| Topic | Position |
|-------|----------|
| Productivity | EF Core maps well to domain entities and use cases. |
| Migrations | Use EF migrations as the schema evolution mechanism; keep them reviewable and module-aware. |
| Change tracking | Appropriate for transactional aggregates (stay, folio, room status). |
| Transactions | Use EF/`IDbContextTransaction` (and ambient transactions only when a use case truly spans modules). Prefer completing a use case inside one module when possible. |
| Testing | Prefer repository/unit-of-work abstractions at application boundaries. Integration tests against PostgreSQL for persistence mapping. Avoid testing EF itself. |
| Query performance | Profile before switching tools. Indexes and query shape matter more than ORM brand. |
| Complex reporting | Likely later; may use SQL views or Dapper then. Do not build a warehouse now. |
| Raw SQL escape hatch | Available immediately via EF Core when a query is clearer as SQL. |
| Dapper coexistence | Allowed later with justification (measured slowness, awkward EF translation, read-only reporting). |

### Modular persistence rules (style only)

- No business modules are created by this ADR.
- When modules exist, prefer **module-owned persistence mapping** (or clearly bounded configurations) over one immortal god context.
- One physical PostgreSQL database is the bootstrap model. Ownership is **logical** initially.
- Do **not** require database-per-module, schema-per-module, or distributed transactions at bootstrap.
- Domain entities must not depend on EF Core types.

---

## Consequences

### Positive

- Fast, consistent persistence for evolving transactional models.
- Migrations provide an auditable schema history.
- A later SQL/Dapper path does not require throwing EF away.

### Negative

- Careless lazy loading and N+1 queries can hurt; conventions and reviews must watch this.
- Reporting queries that join many modules can pressure module boundaries — that is a design smell, not a reason to start with Dapper.

---

## Risks

| Risk | Mitigation |
|------|------------|
| EF leaking into domain | Persistence implementations live in infrastructure; domain stays POCO/domain model. |
| God DbContext | Split by module when the first real modules exist. |
| Premature Dapper | Require a measured problem and a review note, not taste. |
| Migration conflicts in a monolith | Linearize migrations; one integration pipeline when CI exists. |

---

## Revisit Conditions

- Reporting workloads make EF-translated SQL consistently worse than hand-written SQL after indexing.
- A module needs a dedicated read store (then consider SQL/views first, not a new database product).
- EF Core 10 approaches end of support.

---

## Date

2026-08-17

---

## Related Documents

- [ADR-001 Architecture Style](ADR-001-Architecture-Style.md)
- [ADR-002 Backend Platform](ADR-002-Backend-Platform.md)
- [ADR-004 Primary Database](ADR-004-Primary-Database.md)
- [Testing Strategy](../../engineering/TESTING_STRATEGY.md)
