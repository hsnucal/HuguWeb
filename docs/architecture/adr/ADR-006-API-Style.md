# ADR-006: API Style

## Status

Accepted

Accepted by Product Owner and CTO on 2026-08-17 (Sprint 0.3A Architecture Freeze).

---

## Context

HuGuWeb is API-first. The first client is a web ERP SPA (ADR-003). Future clients may include an employee mobile app and third-party integrations (channel managers, payments, POS, accounting — typically **integrate**, not build).

Hotel operations are CRUD plus workflows (check-in, room status changes, folio postings), not a public graph of arbitrarily nested documents.

---

## Problem

Should HuGuWeb’s external API be REST-first, GraphQL-first, or gRPC-first? Should multiple styles be offered at bootstrap?

---

## Decision

We will use a **REST-first JSON HTTP API + OpenAPI** as the only public API style at bootstrap.

Do **not** introduce GraphQL or gRPC at bootstrap.

Practical conventions (to be refined at implementation, not frozen here):

- Resource-oriented endpoints for entities and workflow commands (`POST .../check-in`, not only generic PATCH everywhere)
- OpenAPI description generated from the ASP.NET Core app
- Problem Details (`application/problem+json`) for errors
- Explicit API versioning when the first breaking change is imminent — not a versioning science project on day one

---

## Alternatives Considered

| Alternative | Outcome | Reason |
|-------------|---------|--------|
| REST-first JSON HTTP | Accepted | Matches browser + future mobile + third-party integrations. Easy to debug, cache at HTTP layer if ever needed, and document. |
| GraphQL-first | Rejected | Flexible queries help BFF-style UIs; they also add a large attack/complexity surface (authorization per field, N+1, caching). ERP clients are owned; we can shape REST DTOs. |
| gRPC-first | Rejected | Excellent for service-to-service internals. Awkward as the primary browser API. No microservices to talk to. |
| REST + GraphQL at bootstrap | Rejected | Two contracts, two auth stories, two test suites. |

### Fit

| Need | REST | GraphQL | gRPC |
|------|------|---------|------|
| ERP CRUD / workflows | Natural (resources + commands) | Possible; workflow commands are a poor GraphQL fit | Possible; not browser-native |
| Browser client | Fetch/XHR, trivial | Extra client stack | grpc-web complexity |
| Future mobile | Standard HTTP | Works; extra client | Possible; more mobile plumbing |
| Third-party integrations | Industry default | Less common for PMS partners | Rare for hotel vendors |
| Debugging | curl, browser, logs | Playground helps; HTTP semantics blur | Less approachable |
| Versioning | URL or header versioning | Schema evolution / deprecation | Protobuf compatibility |
| Operational simplicity | Highest | Lower | Lower as the *public* API |

If a future internal extraction needs gRPC between processes, that can be added **inside** the platform without changing the public REST API.

---

## Consequences

### Positive

- One contract for web, mobile, and integrations.
- Aligns with “show work” via workflow-oriented endpoints rather than exposing internal modules as the API’s organizing principle — though URL structure may still follow bounded contexts internally.
- Straightforward authorization on routes and handlers (ADR-008).

### Negative

- Chatty UIs may need purpose-built read DTOs (BFF-style endpoints on the *same* API), not a second protocol.
- Over-fetching is possible; fix with specific read models, not GraphQL by default.

---

## Risks

| Risk | Mitigation |
|------|------------|
| REST becomes a dump of internal entities | Design DTOs and commands per use case. |
| Unversioned breaking changes | Additive changes first; version when breaking. |
| Multiple API styles later “just in case” | Require an ADR to add GraphQL or gRPC. |

---

## Revisit Conditions

- Multiple independent clients need radically different shapes of the same graph and REST aggregation becomes a bottleneck.
- A high-volume internal service extraction needs gRPC.
- A major partner mandates a non-REST protocol (treat as integration adapter, not a platform rewrite).

---

## Date

2026-08-17

---

## Related Documents

- [ADR-001 Architecture Style](ADR-001-Architecture-Style.md)
- [ADR-002 Backend Platform](ADR-002-Backend-Platform.md)
- [ADR-003 Frontend Architecture](ADR-003-Frontend-Architecture.md)
- [Build vs Integrate](../../product/BUILD_VS_INTEGRATE.md)
