# ADR-004: Primary Database

## Status

Accepted

Accepted by Product Owner and CTO on 2026-08-17 (Sprint 0.3A Architecture Freeze).

---

## Context

HuGuWeb is a transactional hotel operations platform. Expected workloads are relational: reservations, rooms, stays, folio lines, users, and later possibly inventory and other back-office records. Consistency of stay/room/folio data matters more than hypothetical planet-scale write throughput.

The product must remain cloud-portable. A cloud vendor is **not** selected (ADR-009). Team familiarity with Microsoft SQL Server is relevant but must not be the sole reason for selection.

---

## Problem

Should HuGuWeb’s system of record be **PostgreSQL** or **Microsoft SQL Server**?

---

## Decision

We will use **PostgreSQL 18** as the primary relational database.

Frozen constraints:

- **One primary relational database initially.**
- **No database-per-module** at bootstrap.
- **No schema-per-module requirement** at bootstrap.
- Module ownership may remain **logical** initially. Physical isolation may be strengthened later only when justified.
- **Do not implement multi-tenancy.**

Recommended major version: **PostgreSQL 18** (current stable major as of 2026-08-17; latest patch in the 18.x line at implementation time). PostgreSQL 19 is in beta and must **not** be used.

Do not pin patch versions in this ADR.

---

## Alternatives Considered

| Alternative | Outcome | Reason |
|-------------|---------|--------|
| PostgreSQL 18 | Accepted | Strong relational/transactional engine, no license cost, excellent cloud portability, JSONB when useful, mature EF Core provider (Npgsql), long support window for major 18 (final release scheduled November 2030 per PostgreSQL versioning policy). |
| Microsoft SQL Server / Azure SQL | Rejected as default | Excellent .NET tooling and transactional strength, but licensing cost (except limited editions) and stronger gravity toward Microsoft/Azure commercial packaging. Not required for HuGuWeb’s current stage. |
| NoSQL as primary store | Rejected | ERP/PMS data is relational and consistency-sensitive. |
| SQLite as primary | Rejected | Acceptable only as an ephemeral local convenience, not as the hotel system of record. |

### Comparison

| Criterion | PostgreSQL | SQL Server |
|-----------|------------|------------|
| Relational ERP workloads | Excellent | Excellent |
| Transactional consistency | ACID, proven | ACID, proven |
| Reporting / indexing | Mature SQL, rich indexes, extensions | Mature SQL, excellent tooling (SSMS/SSRS) |
| JSON where useful | JSONB is first-class | JSON support exists; historically less central |
| Licensing / cost | Open source; operational cost only | Commercial licensing unless using constrained editions / Azure SQL pricing |
| Cloud portability | Azure, AWS, GCP, self-host equally normal | Best on Azure; elsewhere is possible but less natural |
| Tooling | pgAdmin, psql, cloud consoles; slightly less “Visual Studio native” | SSMS / Azure Data Studio; strongest Microsoft DX |
| Developer experience | Excellent; slightly more ops learning if the team is SQL Server-native | Highest if the team is already SQL Server-native |
| EF Core support | Npgsql provider, production-proven | First-party provider, excellent |
| Scaling | Vertical + read replicas; partitioning later | Vertical + read replicas / elastic pools on Azure |
| Operational maturity | Extremely high | Extremely high |
| Future multi-property | Row-level security and schemas are available later; not a reason to implement multi-tenancy now | Equivalent patterns exist |

SQL Server is not a weak database. It is rejected as the *default* because PostgreSQL meets the same transactional needs with better cost and portability for a startup that has not chosen a cloud and may run on-prem or in more than one cloud over time.

If a future enterprise customer *requires* SQL Server, EF Core’s provider model makes a port possible. That is not a reason to start there.

---

## Consequences

### Positive

- No database license in the critical path of a pilot.
- Same engine on a developer laptop, a VM, Azure Database for PostgreSQL, or Amazon RDS.
- JSONB available for integration payloads without abandoning relational modeling.
- Aligns with “do not choose cloud yet.”

### Negative

- If the team is SQL Server-native, initial DBA muscle memory will differ (backup, explain plans, extensions).
- Some Microsoft-only features (SQL Agent, SSIS, CLR) are not the path; that is desirable.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Team friction vs SQL Server | Use EF Core for the common path; document a small operational runbook when implementation starts. |
| Accidental Azure lock-in later | Do not adopt Azure-only database features (Cosmos, Synapse) as the system of record. |
| Premature multi-tenant schema design | Single database, single-property first. Property as an explicit column/concept where data is property-scoped. No tenant-per-database, no tenant infrastructure, and no schema-per-module requirement now. |
| JSONB overuse | Prefer relational columns for core hotel facts (dates, room ids, amounts). JSON for genuinely schemaless integration payloads. |

---

## Revisit Conditions

- A paying customer or regulator mandates SQL Server.
- Licensing or hosting evidence reverses the cost/portability argument.
- PostgreSQL 18 approaches end of life (currently 2030) — plan a major upgrade, not a vendor switch, unless evidence changes.

---

## Date

2026-08-17

---

## Related Documents

- [ADR-001 Architecture Style](ADR-001-Architecture-Style.md)
- [ADR-005 Data Access](ADR-005-Data-Access.md)
- [ADR-009 Cloud Strategy](ADR-009-Cloud-Strategy.md)
- [Target Customer](../../product/TARGET_CUSTOMER.md)
