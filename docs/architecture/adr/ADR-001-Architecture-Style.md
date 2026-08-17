# ADR-001: Architecture Style

## Status

Accepted

Accepted by Product Owner and CTO on 2026-08-17 (Sprint 0.3A Architecture Freeze).

---

## Context

HuGuWeb is a hospitality-first ERP / PMS platform in product discovery. The MVP is not frozen. The initial discovery/pilot focus is independent mid-size hotels with cross-department operational complexity. Long-term growth may include larger hotels, multi-property operations, and hotel groups.

Engineering principles require Clean Architecture, SOLID, high cohesion, low coupling, testability, auditability, API-first design, and a first-class **change-safety** rule: a bug fix in one business area should have the smallest reasonable impact on unrelated areas.

The team is small. Architecture must remain operable by a small team and understandable by a single developer. Premature distributed systems are explicitly discouraged.

Business modules (Reservation, Housekeeping, Finance, and others) are **not** defined in this ADR. This decision evaluates the architectural *style* only.

---

## Problem

Which application architecture style should HuGuWeb adopt as its technical foundation so that:

- hotel operational workflows can be added incrementally as MVP scope is approved
- changes remain localized
- deployment and local development stay simple
- future scale does not require a rewrite
- the system does not start with enterprise infrastructure it cannot yet justify

---

## Decision

We will use a **Modular Monolith with Clean Architecture boundaries**.

Frozen constraints:

- **One deployable backend initially.** A single ASP.NET Core host. No microservices at bootstrap. No premature distributed architecture.
- **Explicit module boundaries.** Each module owns its application and domain logic. Dependencies flow inward toward domain rules. The host composes modules and infrastructure.
- **Modules depend on contracts, not internals.** Cross-module communication uses explicit internal contracts, not unrestricted shared code or another module’s persistence model.
- **Do not define final business modules in this ADR.** Reservation, Housekeeping, Finance, and similar names remain examples only until product scope is approved.

A conceptual shape (modules named only as examples, not an approved module list):

```text
HuGuWeb
 ├── Modules            (added when product scope is approved)
 ├── BuildingBlocks     (small shared kernel; keep thin — see guardrail below)
 └── Host               (composition root, API, auth wiring)
```

This decision does **not** create that structure yet and does **not** freeze business modules.

### BuildingBlocks / Common guardrail

A shared `BuildingBlocks` / `Common` area must **not** become a dumping ground.

Only add a shared primitive when **all** of the following are true:

- at least two real, approved modules need it
- the concept is truly cross-cutting
- ownership is clear

Prefer duplication over premature abstraction when the abstraction is not proven. Do **not** create the BuildingBlocks project in this sprint.

### Database ownership guardrail

Module ownership is initially **logical**, not necessarily physically isolated.

At bootstrap, do **not** require:

- database-per-module
- PostgreSQL schema-per-module
- distributed transactions

One primary relational database is the initial model. Physical isolation (separate schemas, contexts, or later extraction) may be strengthened only when justified. See [ADR-004](ADR-004-Primary-Database.md).

### Multi-property guardrail

- Initial product focus remains **single-property independent mid-size hotels**.
- Multi-property remains **strategic future scope**.
- Initial architecture should avoid making future multi-property impossible (for example, do not hard-code “there is only ever one hotel” into every design).
- Do **not** implement tenant infrastructure now.

---

## Alternatives Considered

| Alternative | Outcome | Reason |
|-------------|---------|--------|
| Traditional layered monolith (Presentation / Application / Domain / Infrastructure only) | Rejected as the primary style | Simple to start, but layers alone do not isolate Housekeeping from Finance. As departments grow, “Application” and “Domain” become dumping grounds. Bug-fix blast radius increases. |
| Modular monolith with Clean Architecture boundaries | Accepted | Preserves one deployable while enforcing bounded ownership, contracts, and testability. Matches change-safety and “architecture is a tool” principles. |
| Microservices | Rejected for this stage | Independent mid-size hotel pilots do not justify network boundaries, distributed data, separate deployments, or operational overhead. Growth *may* happen later; that is not evidence to start distributed. |

### Option A — Traditional Layered Monolith

| Criterion | Assessment |
|-----------|------------|
| Simplicity | Highest at day one |
| Module isolation | Weak. Layers cut *through* business areas rather than around them |
| Long-term maintainability | Degrades as hotel departments accumulate in shared layers |
| Bug impact | A shared service or table change can touch unrelated workflows |
| Growth risk | Becomes a “big ball of mud” unless module boundaries are added later at higher cost |

Acceptable only as a very short bootstrap *before* the first real bounded context exists. It is not the target style.

### Option B — Modular Monolith (accepted)

| Criterion | Assessment |
|-----------|------------|
| Bounded module ownership | Each module owns use cases, domain rules, and persistence mapping for its area |
| Internal contracts | Other modules depend on published interfaces/events, not internal entities |
| Dependency isolation | Architecture tests can forbid illegal references |
| Database ownership | One physical database initially; ownership is logical. Schema-per-module and database-per-module are not required at bootstrap. |
| Future extraction | A module that truly needs independent scale can be extracted later because boundaries already exist |
| Testability | Domain and application tests run per module without standing up the whole system |
| Deployment simplicity | One API process. No service mesh, no distributed transactions |

### Option C — Microservices

Not justified at current startup stage.

Microservices would add independent deployability and team scaling that HuGuWeb does not have, in exchange for:

- distributed failure modes
- data consistency problems across reservations, rooms, housekeeping, and folio
- operational burden (discovery, tracing across services, versioned contracts over the network)
- slower local development

Hotel workflows are inherently connected (checkout → room status → housekeeping → next arrival). Cutting those with network calls *before* the workflows are even defined would encode the wrong boundaries.

---

## Consequences

### Positive

- One process and one deployment match a small team and early pilots.
- Module isolation supports the change-safety principle: a Housekeeping defect can be fixed without destabilizing Finance, *if* contracts and data ownership are respected.
- Clean Architecture keeps domain rules free of EF Core, HTTP, and vendor APIs.
- Future extraction of a module is possible without a rewrite of the whole product.
- Local development stays simple: API + frontend + PostgreSQL.

### Negative

- Requires discipline. A modular monolith that ignores boundaries is just a monolith with folders.
- Shared kernel can rot if “convenient” types are dumped into BuildingBlocks — the guardrail above exists to prevent that.
- A single database still allows accidental coupling via tables and joins unless logical ownership rules are enforced.
- In-process calls can hide overly chatty cross-module designs until extraction is attempted.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Folder-only modularity | Enforce dependency rules with architecture tests as soon as modules exist. Review cross-module PRs for contract violations. |
| Premature module explosion | Do not create business modules until product scope is approved. Start with Host + thin BuildingBlocks only when a shared primitive is justified. |
| Shared database coupling | One database is allowed. Ownership is logical at bootstrap. Shared tables should be rare. Prefer module-owned tables and explicit read models/contracts for cross-module queries. Do not require schema-per-module yet. |
| BuildingBlocks dumping ground | Add shared types only when two approved modules need a proven cross-cutting concept. Prefer duplication until then. |
| Over-engineering the first slice | First vertical slice should prove the pattern, not invent a framework. |
| Future multi-property surprise | Treat Property as an explicit domain concept in data that is property-scoped. Do **not** implement multi-tenancy, hotel-group hierarchy, or tenant isolation in this ADR. |

---

## Future scale without a rewrite

Scale is handled in this order, only when evidence requires it:

1. Vertical slice inside the monolith (code, indexes, queries).
2. In-process background work for slow or scheduled tasks.
3. Split read-heavy reporting queries (see ADR-005) without splitting the app.
4. Extract a module to a separate process only when that module has independent scale, team, or failure isolation needs.

This path does not require starting with Kubernetes, brokers, or microservices.

---

## Revisit Conditions

- A module cannot be changed without routinely breaking unrelated modules despite architecture tests.
- Independent deployability of one area becomes a proven operational need (not a hypothetical).
- Team size and release cadence make a single deployable a bottleneck.
- Product evidence shows a workflow that cannot be expressed with in-process contracts without distributed consistency requirements.

---

## Date

2026-08-17

---

## Related Documents

- [ADR-002 Backend Platform](ADR-002-Backend-Platform.md)
- [ADR-004 Primary Database](ADR-004-Primary-Database.md)
- [ADR-005 Data Access](ADR-005-Data-Access.md)
- [Engineering Principles](../../engineering/ENGINEERING_PRINCIPLES.md)
- [Architecture README](../README.md)
- [Technology Decisions](../TECHNOLOGY_DECISIONS.md)
- [Product Vision](../../product/PRODUCT_VISION.md)
- [Target Customer](../../product/TARGET_CUSTOMER.md)
