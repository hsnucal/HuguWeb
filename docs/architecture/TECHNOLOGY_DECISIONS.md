# Technology Decisions

> **Status:** Sprint 0.3A Architecture Freeze. Listed architecture and technology decisions below are **Accepted**. Items in Open Decisions remain deferred.

This document summarizes accepted architecture and technology decisions. Detailed reasoning lives in the ADRs listed below.

Related: [Architecture README](README.md) · [ADR index](adr/README.md)

---

## Decision Matrix

| Area | Decision | Alternatives | Status |
|------|----------|--------------|--------|
| Architecture style | Modular monolith with Clean Architecture boundaries | Traditional layered monolith; microservices | **Accepted** — [ADR-001](adr/ADR-001-Architecture-Style.md) |
| Backend | ASP.NET Core / .NET 10 LTS | Node.js/TypeScript; Java/Spring Boot; .NET 8/9 | **Accepted** — [ADR-002](adr/ADR-002-Backend-Platform.md) |
| Frontend | React 19 SPA + Vite 8 + TypeScript (no Next.js, no Blazor) | Next.js; Blazor | **Accepted** — [ADR-003](adr/ADR-003-Frontend-Architecture.md) |
| Primary database | PostgreSQL 18 | Microsoft SQL Server / Azure SQL | **Accepted** — [ADR-004](adr/ADR-004-Primary-Database.md) |
| Data access | EF Core 10 for transactional persistence; raw SQL where justified | Dapper-first; EF-only dogma | **Accepted** — [ADR-005](adr/ADR-005-Data-Access.md) |
| API style | REST-first JSON HTTP + OpenAPI | GraphQL-first; gRPC-first | **Accepted** — [ADR-006](adr/ADR-006-API-Style.md) |
| Authentication | ASP.NET Core Identity inside HuGuWeb; cookie auth for web SPA | External OIDC IdP; Duende/IdentityServer at bootstrap | **Accepted** — [ADR-007](adr/ADR-007-Authentication-Strategy.md) |
| Authorization | Permission-based; roles as DB bundles over Identity | Static role-name checks; IdentityRole as hotel IAM; ABAC engine | **Accepted** — [ADR-008](adr/ADR-008-Authorization-Strategy.md), [ADR-010](adr/ADR-010-Database-Managed-Authorization.md) |
| Cloud strategy | Provider-neutral initial architecture; no vendor selected | Azure-first; AWS-first; Kubernetes | **Accepted** (strategy only) — [ADR-009](adr/ADR-009-Cloud-Strategy.md) |
| ORM coexistence | EF Core now; raw SQL escape hatch; Dapper later if measured | Dapper at bootstrap | **Accepted** — ADR-005 |
| Background jobs | Not at bootstrap; in-process hosted services first when needed | Kafka; RabbitMQ; Hangfire at bootstrap | Open — deferred |
| Caching | No distributed cache at bootstrap | Redis from day one | Open — deferred |
| Observability vendor | None paid; OpenTelemetry-compatible foundation | Datadog/New Relic/etc. at bootstrap | Open — vendor deferred; foundation accepted |
| Multi-tenancy | Not implemented; Property as explicit concept only | Full tenant isolation; hotel-group hierarchy | Open — not accepted |
| Final business module boundaries | Not defined | — | Open |
| Mobile technology | Not selected | — | Open |
| Cloud provider | Not selected | Azure; AWS; other | Open |
| Message broker | Not selected | — | Open |
| Background job library | Not selected | — | Open |
| External OIDC vendor | Not selected | — | Open |
| CI/CD platform | Not selected | — | Open |

---

## Required at Bootstrap

When implementation is later authorized, the lean runtime is:

```text
React SPA  →  ASP.NET Core API  →  PostgreSQL
```

Required technical foundation (still not to be scaffolded in this sprint):

- React SPA
- ASP.NET Core API
- PostgreSQL
- EF Core
- Identity
- permission-ready authorization
- structured logging
- global error handling
- health checks
- OpenAPI
- basic OpenTelemetry-compatible foundation
- automated tests

Also required as architectural shape (not as empty module projects):

- Modular monolith hosting model (Host + thin BuildingBlocks only when justified; modules added when product-approved)
- Business auditability as a **design requirement** (who / what / when / property context) — not a separate product to build first

---

## Add When Needed

| Component | Add when | Still avoid until then |
|-----------|----------|------------------------|
| Object storage | Documents, photos, or attachments are in approved scope | Custom file framework |
| Background job infrastructure | Notifications, scheduled reports, retries, reminders are real | Message brokers as the first step |
| Email provider | Invites, reset, and operational notifications leave the server | Homegrown mail infrastructure |
| External OIDC / SSO | Hotel group or customer directory requires it | Picking a vendor now |
| Redis / distributed cache | Multiple API instances + proven hot-path need | Caching for its own sake |
| Dapper | Measured reporting/read pain after indexes | Second ORM “just in case” |
| Dedicated worker process | In-process jobs affect API latency or reliability | Kubernetes jobs platform |
| Message broker | Multiple independent consumers, durable integration fan-out, or proven volume | Kafka “because events” |
| Mobile-specific auth flows | Employee mobile client is approved | Building a full IAM suite |
| OpenTelemetry exporter + dashboard | Production/pilot needs centralized traces/metrics | Paid vendor lock-in |
| Docker/compose | Team needs repeatable local Postgres/API | Kubernetes |
| CI/CD | First mergeable application code exists | Pipeline theater before code |

---

## Explicitly Avoid for Now

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
- Distributed transactions / sagas
- Next.js as the ERP shell
- Defining final business modules in code
- Infrastructure added only for hypothetical scale

---

## BuildingBlocks / Common Guardrail

A shared `BuildingBlocks` / `Common` area must **not** become a dumping ground.

Only add shared primitives when:

- at least two real approved modules need them
- the concept is truly cross-cutting
- ownership is clear

Prefer duplication over premature abstraction when the abstraction is not proven.

Do **not** create the BuildingBlocks project in this sprint.

---

## Database Ownership Guardrail

Module ownership is initially **logical**, not necessarily physically isolated.

Do **not** require at bootstrap:

- database-per-module
- PostgreSQL schema-per-module
- distributed transactions

Physical isolation may be strengthened later only when justified.

---

## Multi-Property Guardrail

- HuGuWeb supports **multiple Properties in one Organization** on **shared tables** (Accepted: [ARCH-FOUNDATION-001](foundation/ARCH-FOUNDATION-001.md), [TENANCY](foundation/TENANCY.md)).
- Isolation is `OrganizationId` / `PropertyId` + membership. Not table/schema/database-per-hotel.
- Property-scoped operations require an explicit Property context. Organization-wide membership does **not** infer a pilot/default Property.
- Menus derive from effective permissions. Queries derive from tenant context.

These terms remain distinct:

| Term | Meaning | Current architecture |
|------|---------|----------------------|
| **Tenant** | Isolation for a customer installation | One shared DB; not a SaaS tenant platform |
| **Organization** | Legal/commercial entity | First-class Workforce row; membership `OrganizationId` |
| **Hotel / Property** | One operational site | First-class row; `TimeZoneId`; Property-scoped domains require explicit context |
| **Hotel Group** | Portfolio of properties | Represented as one Organization with many Properties for Development; no extra group hierarchy table |

**How much future readiness is enough?**

- Enough: explicit Property identifiers, shared tables, indexes on tenant keys, no silent default hotel.
- Too much: microservices, row-level security products, db-per-tenant, brokers.

---

## Application Hosting Model

### Bootstrap (required)

```text
Web Frontend
      ↓
ASP.NET Core API
      ↓
PostgreSQL
```

Single API instance is assumed initially.

### Supporting components

| Component | Classification |
|-----------|----------------|
| Web frontend + API + PostgreSQL | **Required at bootstrap** |
| Health checks / structured logs / global error handling / OpenAPI | **Required at bootstrap** (when app exists) |
| OpenTelemetry-compatible foundation | **Required at bootstrap** (when app exists) |
| Automated tests | **Required at bootstrap** (when code exists) |
| Object storage | **Add when needed** |
| Background worker (in-process first) | **Add when needed** |
| Distributed cache (Redis) | **Add when needed** — only with evidence |
| Message broker | **Add when needed** |
| Kubernetes / service mesh | **Explicitly avoid for now** |

---

## Background Jobs

Not required at bootstrap. There are no notifications, reports, or integration polls to run.

When the first real async need appears:

1. `IHostedService` / `BackgroundService` in the same host
2. A simple in-process scheduler/job library if recurring work becomes painful
3. A separate worker process if the API must be isolated from job load
4. A broker only if multiple consumers and durable delivery are proven needs

Do not select Kafka, RabbitMQ, or similar because “ERP might be asynchronous.” The background job **library** remains open.

---

## Caching

**No distributed cache initially.** Caching technology remains an open decision.

- Single instance: correctness of room status and folio matters more than cache.
- In-memory cache only for truly safe, rarely changing reference data — and only when a measured need exists.
- Redis (or equivalent) later if multiple instances share hot read paths.

Premature caching in an ERP causes stale operational state (room readiness, stock, folio). That is a product defect, not an optimization.

---

## Observability Baseline

Initial foundation (no paid vendor selected):

| Concern | Bootstrap recommendation |
|---------|--------------------------|
| Structured logging | JSON logs via `ILogger` (Serilog or built-in providers at implementation time) |
| Correlation / request IDs | Middleware; include id on requests, logs, and error responses |
| Error handling | Global handler; RFC 7807 Problem Details; no stack traces to clients |
| Health checks | ASP.NET Core health checks (self + PostgreSQL when wired) |
| Metrics | .NET meters; export via OpenTelemetry when a backend exists |
| Tracing | `Activity` / OpenTelemetry instrumentation; export later |

Prefer **OpenTelemetry-compatible** APIs from the start so a vendor (or Grafana/Jaeger/OTLP collector) can be attached without rewriting logs.

Do not select a paid observability product now.

Distinguish these concerns; do **not** implement a business audit system yet:

| Kind | Purpose | Bootstrap stance |
|------|---------|------------------|
| **Technical logs** | Diagnose failures, performance, exceptions | Required with the first API |
| **Security audit** | Sign-in failures, lockouts, permission denials | Use Identity + authZ logging when auth exists |
| **Business audit trail** | Who changed what business fact, when, in which property/context | Design requirement for critical mutations as those features appear. Prefer explicit audit records — not “the log file is the audit.” Do not implement a business audit system now. |

Do not use event sourcing to satisfy audit.

When the first mutable business entities exist, include actor/time metadata (`created`/`modified` by and at) as a minimum; add a proper change trail when the first high-risk action ships.

---

## Testing Baseline

Do **not** optimize for coverage percentage. Frameworks remain selectable at implementation.

| Category | When |
|----------|------|
| **Unit tests** | At project bootstrap |
| **Architecture tests** | At project bootstrap |
| **PostgreSQL integration tests** | When persistence is introduced |
| **API integration tests** | When API behavior exists |
| **End-to-end tests** | Only after real critical workflows exist |

Goal: strong regression protection without a slow test bureaucracy. One focused architecture test project is worth more than a large E2E suite of empty screens.

See [Testing Strategy](../engineering/TESTING_STRATEGY.md).

---

## Candidate Repository Structure

**Do not create this structure in this sprint.** Candidate only.

### Recommended (lean modular monolith)

Grow into this as modules are product-approved. Do **not** pre-create empty business module projects.

```text
HuguWeb/
├── docs/                          # existing documentation
├── src/
│   ├── backend/
│   │   ├── HuGuWeb.Host/          # composition root, API, Identity wiring
│   │   ├── HuGuWeb.BuildingBlocks/
│   │   └── modules/               # add only when a module is approved
│   └── frontend/
│       └── web/                   # React SPA (Vite + TypeScript)
└── tests/
    ├── HuGuWeb.Architecture.Tests/
    ├── HuGuWeb.Unit.Tests/
    └── HuGuWeb.Integration.Tests/
```

Host references modules; modules must not reference Host. BuildingBlocks stays thin and follows the guardrail above.

### Simpler alternative (valid for the first vertical slice)

If the first implementation sprint is a thin slice before any bounded context is approved:

```text
HuguWeb/
├── docs/
├── src/
│   ├── backend/HuGuWeb.Api/
│   └── frontend/web/
└── tests/
```

Split into Host / BuildingBlocks / modules **as soon as** a second business area would otherwise share a dumping-ground project. Do not wait until the ball of mud exists.

Avoid dozens of empty class libraries named after unapproved ERP modules.

---

## Technology Versions

Stable, supported production majors only. Patches are not pinned here.

| Technology | Recommended major | Lifecycle reason | Verified |
|------------|-------------------|------------------|----------|
| .NET / ASP.NET Core | **10 LTS** | Current LTS; support through **14 Nov 2028** | Yes — [Microsoft .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) (2026-08-17) |
| EF Core | **10** | LTS with .NET 10; support through **10 Nov 2028** | Yes — [EF Core releases](https://learn.microsoft.com/en-us/ef/core/what-is-new/) |
| PostgreSQL | **18** | Current stable major; PG 19 is beta | Yes — [postgresql.org](https://www.postgresql.org/) (18.6 as of 13 Aug 2026) |
| React | **19** | Current stable (19.2.x) | Yes — [react.dev/versions](https://react.dev/versions) |
| Vite | **8** | Current stable SPA toolchain | Yes — [vite.dev](https://vite.dev/blog/announcing-vite8) / npm (8.2.x as of Aug 2026) |
| Node.js (frontend tooling) | **24 LTS** | Active LTS; EOL **30 Apr 2028** | Yes — [Node.js release schedule](https://github.com/nodejs/release) |
| Next.js | Not selected | Not recommended for this ERP | n/a |
| TypeScript | Current stable 5.x at implementation | Patch not pinned | **CTO verification** of exact minor at scaffold time |

If any of the above cannot be re-verified at implementation time, treat the major as the decision and confirm the latest patch with the vendor.

---

## ADR Index

| ADR | Topic | Status |
|-----|--------|--------|
| [ADR-001](adr/ADR-001-Architecture-Style.md) | Architecture style | Accepted |
| [ADR-002](adr/ADR-002-Backend-Platform.md) | Backend platform | Accepted |
| [ADR-003](adr/ADR-003-Frontend-Architecture.md) | Frontend architecture | Accepted |
| [ADR-004](adr/ADR-004-Primary-Database.md) | Primary database | Accepted |
| [ADR-005](adr/ADR-005-Data-Access.md) | Data access | Accepted |
| [ADR-006](adr/ADR-006-API-Style.md) | API style | Accepted |
| [ADR-007](adr/ADR-007-Authentication-Strategy.md) | Authentication | Accepted |
| [ADR-008](adr/ADR-008-Authorization-Strategy.md) | Authorization | Accepted |
| [ADR-009](adr/ADR-009-Cloud-Strategy.md) | Cloud strategy (provider-neutral; vendor not selected) | Accepted |
| [ADR-010](adr/ADR-010-Database-Managed-Authorization.md) | Database-managed membership authorization | Accepted |

---

## Architecture Quality Check

| Question | Answer |
|----------|--------|
| Are we over-engineering? | No. One API, one SPA, one database. No brokers, K8s, or microservices. |
| Fashionable tech? | Next.js, GraphQL, and Kafka were considered and rejected. |
| Does every component solve a current problem? | Bootstrap components do. Everything else is classified Add/Avoid. |
| Can a single developer operate this? | Yes. |
| Can a Housekeeping bug be fixed without breaking Finance? | Yes, *if* module contracts and architecture tests are enforced when modules exist. |
| Can modules be tested independently? | Yes — that is a primary reason for the modular monolith. |
| Can we grow toward larger hotels without enterprise infra first? | Yes: property id → later isolation; extract a module only with evidence. |
| Is deployment understandable? | Static UI + API + PostgreSQL. |
| Is local development simple? | API + SPA + local PostgreSQL. No cluster required. |

---

## Date

2026-08-17
