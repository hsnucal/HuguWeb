# Architecture

This directory contains HuGuWeb architecture documentation and the Architecture Decision Record (ADR) system.

---

## Current Status

**Sprint 0.3A Architecture Freeze is Accepted.** Product Owner and CTO approved the architecture and technology baseline on 2026-08-17.

HuGuWeb remains in product discovery and foundation stage. Application code, scaffolding, and stack installation have **not** started. Accepted ADRs authorize the *direction*; they do not authorize implementation in this sprint.

See [TECHNOLOGY_DECISIONS.md](TECHNOLOGY_DECISIONS.md) for the accepted matrix and remaining open decisions.

---

## Accepted Architectural Direction

| Topic | Status |
|-------|--------|
| Modular Monolith with Clean Architecture boundaries | **Accepted** — [ADR-001](adr/ADR-001-Architecture-Style.md) |
| ASP.NET Core on .NET 10 LTS | **Accepted** — [ADR-002](adr/ADR-002-Backend-Platform.md) |
| React 19 + TypeScript + Vite 8 SPA | **Accepted** — [ADR-003](adr/ADR-003-Frontend-Architecture.md) |
| PostgreSQL 18 | **Accepted** — [ADR-004](adr/ADR-004-Primary-Database.md) |
| EF Core 10 | **Accepted** — [ADR-005](adr/ADR-005-Data-Access.md) |
| REST-first JSON HTTP + OpenAPI | **Accepted** — [ADR-006](adr/ADR-006-API-Style.md) |
| ASP.NET Core Identity | **Accepted** — [ADR-007](adr/ADR-007-Authentication-Strategy.md) |
| Permission-based authorization | **Accepted** — [ADR-008](adr/ADR-008-Authorization-Strategy.md) |
| Provider-neutral cloud strategy | **Accepted** — [ADR-009](adr/ADR-009-Cloud-Strategy.md) |
| Microservices | Avoid unless a later ADR justifies — rejected for current stage in ADR-001 |
| Multi-tenancy implementation | Not designed — remains open |
| Event Sourcing / CQRS everywhere | Avoid unless ADR justifies |
| Message brokers / event streaming | Avoid unless ADR justifies |

Final business module boundaries, cloud provider, mobile technology, caching technology, message broker, background job library, external OIDC vendor, observability vendor, and CI/CD platform remain **open**.

See [Engineering Principles](../engineering/ENGINEERING_PRINCIPLES.md) for guardrails and premature-architecture avoidance.

---

## Change Safety

A first-class engineering requirement:

> A change or bug fix in one business area should have the smallest reasonable impact on unrelated business areas.

Architecture should encourage module isolation, explicit contracts, dependency boundaries, and controlled database evolution. Final business module boundaries are **not designed yet**.

---

## Guardrails

### BuildingBlocks / Common

A shared `BuildingBlocks` / `Common` area must not become a dumping ground. Add shared primitives only when at least two real approved modules need them, the concept is truly cross-cutting, and ownership is clear. Prefer duplication over premature abstraction.

### Database ownership

Module ownership is initially **logical**, not necessarily physically isolated. Do not require database-per-module, PostgreSQL schema-per-module, or distributed transactions at bootstrap.

### Multi-property

Initial product focus remains single-property independent mid-size hotels. Multi-property remains strategic future scope. Architecture should avoid making future multi-property impossible. Do **not** implement tenant infrastructure now.

The following concepts are **not** automatically equivalent and must be formally defined before implementation:

- Tenant
- Hotel / Property
- Hotel Group

See [Glossary](../product/GLOSSARY.md) and [Future Scope](../product/FUTURE_SCOPE.md).

---

## Integration Boundaries

External systems (channel managers, payment providers, POS, payroll, etc.) should eventually be isolated from core business logic through appropriate integration boundaries.

Integration vendors are not selected during the foundation stage. See [Product Principles](../product/PRODUCT_PRINCIPLES.md).

---

## Architecture Decision Records (ADRs)

Significant architecture decisions must be documented as ADRs in [`adr/`](adr/).

| Resource | Description |
|----------|-------------|
| [ADR README](adr/README.md) | How to use the ADR system |
| [ADR Template](adr/ADR-TEMPLATE.md) | Template for new ADRs |

Proposed ADRs may be recorded for review. Do not mark them Accepted without Product Owner + CTO approval. Do not create ADRs for trivial implementation choices.

---

## Technology Decisions

The Sprint 0.3A stack and architecture style are **Accepted**. Remaining items (cloud provider, CI/CD, mobile, caching product, brokers, and similar) stay open.

See [TECHNOLOGY_DECISIONS.md](TECHNOLOGY_DECISIONS.md) and [Engineering Principles](../engineering/ENGINEERING_PRINCIPLES.md).

---

## Related Documents

- [Technology Decisions](TECHNOLOGY_DECISIONS.md)
- [Engineering Principles](../engineering/ENGINEERING_PRINCIPLES.md)
- [Testing Strategy](../engineering/TESTING_STRATEGY.md)
- [Product Vision](../product/PRODUCT_VISION.md)
- [Future Scope](../product/FUTURE_SCOPE.md)
