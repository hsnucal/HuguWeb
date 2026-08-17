# Engineering Principles

These principles guide HuGuWeb engineering decisions. They establish standards and constraints—not a technology stack or architecture blueprint.

---

## Core Principles

HuGuWeb must prioritize:

| Principle | Intent |
|-----------|--------|
| **Clean Architecture** | Separate business rules from infrastructure; maintain clear dependency direction |
| **SOLID** | Single responsibility, open/closed, Liskov substitution, interface segregation, dependency inversion |
| **Clean Code** | Readable, intentional, maintainable code over clever abstractions |
| **High cohesion** | Related behavior stays together within well-defined boundaries |
| **Low coupling** | Minimize dependencies between unrelated areas |
| **Explicit boundaries** | Module and layer boundaries must be visible and enforceable |
| **Testability** | Design for automated testing from the start |
| **Maintainability** | Changes should be predictable and localized |
| **Minimal regression surface** | Reduce the blast radius of changes and bug fixes |
| **Minimal bug-fix impact** | A fix in one area should not unnecessarily affect unrelated areas |
| **Clear dependency direction** | Dependencies flow inward toward business rules, not outward toward infrastructure |
| **Separation of business rules from infrastructure** | Domain logic must not depend on databases, frameworks, or external services |
| **API-first thinking** | Design interfaces and contracts before implementation details |
| **Cloud-ready architecture** | Design for deployability and scalability without premature distributed complexity |
| **Security by design** | Security considerations are part of design, not an afterthought |
| **Observability** | Systems must support monitoring, logging, and diagnostics |
| **Auditability** | Business-critical actions must be traceable |
| **Automated testing** | Tests protect business behavior; see [Testing Strategy](TESTING_STRATEGY.md) |
| **Backward-compatible evolution** | Prefer evolutionary changes where practical to reduce disruption |

---

## Architecture Is a Tool, Not the Product

> Architecture is a tool for product delivery, not the product itself.

Good architecture does not mean maximum architecture. Architecture complexity must always be justified by product requirements. Avoid architecture astronautics—infrastructure and patterns introduced without clear product need increase maintenance cost and cognitive load.

---

## Change Safety Principle

HuGuWeb has a first-class engineering requirement:

> A change or bug fix in one business area should have the smallest reasonable impact on unrelated business areas.

Future architecture should encourage:

- Module isolation
- Explicit contracts
- Dependency boundaries
- Regression tests
- Small and focused changes
- Controlled database evolution
- Impact analysis
- Predictable side effects

Do **not** define final business modules yet. This principle guides how those modules will be isolated when product scope is approved.

---

## Modular Monolith (Accepted)

HuGuWeb uses a **Modular Monolith with Clean Architecture boundaries** — a single deployable backend with well-isolated internal modules, explicit contracts, and inward-flowing dependencies. See [ADR-001](../architecture/adr/ADR-001-Architecture-Style.md).

Frozen constraints:

- one deployable backend initially
- explicit module boundaries
- modules depend on contracts, not internals
- no microservices at bootstrap
- no premature distributed architecture

Final business modules are **not** defined yet.

---

## Architecture Guardrails

### BuildingBlocks / Common

A shared `BuildingBlocks` / `Common` area must **not** become a dumping ground.

Only add shared primitives when:

- at least two real approved modules need them
- the concept is truly cross-cutting
- ownership is clear

Prefer duplication over premature abstraction when the abstraction is not proven. Do not create the BuildingBlocks project until implementation is authorized and the need is real.

### Database ownership

Module ownership is initially **logical**, not necessarily physically isolated.

Do not require at bootstrap:

- database-per-module
- PostgreSQL schema-per-module
- distributed transactions

Physical isolation may be strengthened later only when justified.

### Multi-property

- Initial product focus remains single-property independent mid-size hotels
- Multi-property remains strategic future scope
- Initial architecture should avoid making future multi-property impossible
- Do **not** implement tenant infrastructure now

---

## Premature Architecture to Avoid

HuGuWeb should avoid premature adoption of the following unless future requirements and an approved ADR justify them:

- Microservices
- Kubernetes
- Kafka
- RabbitMQ
- Event Sourcing
- CQRS everywhere
- Generic workflow engine
- Redis without evidence
- GraphQL-first
- gRPC-first
- Custom crypto
- Full multi-tenancy platform
- Premature cloud abstraction frameworks
- Distributed systems without evidence
- Unnecessary abstraction layers
- Speculative generic frameworks
- Infrastructure introduced only for hypothetical scale

Introducing any of the above without product justification increases complexity, operational cost, and regression risk.

---

## Accepted Technology Decisions

Sprint 0.3A Architecture Freeze accepted the following. Do not install, scaffold, or implement until a later sprint authorizes implementation.

| Category | Status |
|----------|--------|
| Architecture style | **Accepted** — Modular monolith with Clean Architecture — [ADR-001](../architecture/adr/ADR-001-Architecture-Style.md) |
| .NET version | **Accepted** (.NET 10 LTS) — [ADR-002](../architecture/adr/ADR-002-Backend-Platform.md) |
| Backend framework | **Accepted** (ASP.NET Core) — ADR-002 |
| Frontend | **Accepted** (React 19 SPA + Vite 8 + TypeScript; not Next.js; not Blazor) — [ADR-003](../architecture/adr/ADR-003-Frontend-Architecture.md) |
| Database | **Accepted** (PostgreSQL 18) — [ADR-004](../architecture/adr/ADR-004-Primary-Database.md) |
| Data access | **Accepted** (EF Core 10; raw SQL where justified; no Dapper at bootstrap) — [ADR-005](../architecture/adr/ADR-005-Data-Access.md) |
| API style | **Accepted** (REST-first JSON HTTP + OpenAPI) — [ADR-006](../architecture/adr/ADR-006-API-Style.md) |
| Authentication | **Accepted** (ASP.NET Core Identity in-app) — [ADR-007](../architecture/adr/ADR-007-Authentication-Strategy.md) |
| Authorization | **Accepted** (permission-based, ASP.NET policies; roles as bundles) — [ADR-008](../architecture/adr/ADR-008-Authorization-Strategy.md) |
| Cloud strategy | **Accepted** (provider-neutral; no vendor) — [ADR-009](../architecture/adr/ADR-009-Cloud-Strategy.md) |

See [Technology Decisions](../architecture/TECHNOLOGY_DECISIONS.md).

---

## Technology Decisions Explicitly Deferred

The following remain **open**. Do not treat them as accepted.

| Category | Status |
|----------|--------|
| Final business module boundaries | Not defined |
| Multi-tenancy implementation | Not selected — analysis and guardrails only |
| Mobile technology | Not selected (future scope) |
| Cloud provider | Not selected |
| Caching technology | Not selected — none at bootstrap |
| Message broker | Not selected — none at bootstrap |
| Background job library | Not selected — none at bootstrap |
| External OIDC vendor | Not selected |
| Observability vendor | Not selected — OpenTelemetry-compatible foundation is required at bootstrap |
| CI/CD platform | Not selected |
| Container platform | Not selected |

---

## Related Documents

- [Development Workflow](DEVELOPMENT_WORKFLOW.md)
- [Testing Strategy](TESTING_STRATEGY.md)
- [Architecture](../architecture/README.md)
- [Technology Decisions](../architecture/TECHNOLOGY_DECISIONS.md)
- [ADR Template](../architecture/adr/ADR-TEMPLATE.md)
