# ADR-002: Backend Platform

## Status

Accepted

Accepted by Product Owner and CTO on 2026-08-17 (Sprint 0.3A Architecture Freeze).

---

## Context

HuGuWeb needs an API-first backend that can host a modular monolith, enforce Clean Architecture, support strong testing, security, observability, and long-term maintainability.

The implementation team has strong C# / .NET experience. That is a real constraint, not the only criterion.

HuGuWeb is an authenticated ERP / PMS with transactional hotel workflows, audit needs, and future mobile clients talking to the same API. MVP scope is not frozen; the platform must remain productive while modules are still unknown.

---

## Problem

Which backend platform should HuGuWeb use as its primary application runtime?

---

## Decision

We will use **ASP.NET Core on .NET 10 LTS** as the backend platform.

Use **supported stable production releases only**. Do not adopt preview, RC, or unsupported STS lines for this product.

Recommended major version: **.NET 10** (Long Term Support).

Verified from Microsoft support policy (as of 2026-08-17):

- Original release: 11 November 2025
- Release type: LTS (three years of support)
- End of support: **14 November 2028**
- EF Core 10 follows the same LTS line (see ADR-005)

Do not pin patch versions in this ADR. Use the latest 10.0.x patch at implementation time.

---

## Alternatives Considered

| Alternative | Outcome | Reason |
|-------------|---------|--------|
| ASP.NET Core / .NET 10 LTS | Accepted | Mature, LTS, first-class APIs, DI, testing, security, and modular-monolith hosting. Matches team skill. |
| Node.js / TypeScript | Rejected as primary backend | Strong for JS-centric teams and JSON APIs. Weaker fit for a transactional ERP domain model, compiled architecture tests, and this team’s experience. Would duplicate language across frontend and backend without a compensating product need. |
| Java / Spring Boot | Rejected | Equally capable for enterprise modular monoliths. No team advantage. Adds hiring and toolchain split with no product benefit. |
| .NET 8 LTS | Rejected | Still supported until November 2026, then end of life. Starting a new product on a soon-expiring LTS would force an early upgrade. |
| .NET 9 STS | Rejected | Standard Term Support; not the right default for a new long-lived product when .NET 10 LTS is current. |

### Fit against HuGuWeb requirements

| Requirement | ASP.NET Core / .NET 10 |
|-------------|------------------------|
| Maturity | Production-proven web stack; .NET 10 is current LTS, not preview. |
| Performance | High-throughput Kestrel; sufficient for hotel operational load without distributed tricks. |
| Ecosystem | EF Core, Identity, OpenTelemetry, health checks, testing libraries. |
| API development | Minimal APIs or controllers; OpenAPI; versioning patterns. |
| Dependency injection | Built-in container; explicit composition in Host. |
| Testing | xUnit / NUnit, `WebApplicationFactory`, testable DI. |
| Background jobs | `IHostedService` / `BackgroundService` first; dedicated job library later if needed. |
| Observability | Built-in logging, metrics, `Activity`; OpenTelemetry-compatible. |
| Security | ASP.NET Core Identity, data protection, HTTPS, authN/authZ middleware. Do not implement custom cryptography. |
| Long-term support | LTS through November 2028. |
| Developer productivity | High for this team. |
| Modular monolith | Solution/project boundaries, internal contracts, architecture tests are well-supported. |

Node.js would be reasonable if the team were TypeScript-first and the product were integration-glue rather than a transactional hotel domain. Spring Boot would be reasonable for a Java-first team. Neither is the lean choice here.

---

## Consequences

### Positive

- One language for domain, application, tests, and host.
- Natural mapping to Clean Architecture + modular projects.
- Security and observability primitives exist in-box.
- Cloud-ready without choosing a cloud vendor now.

### Negative

- Frontend remains a separate TypeScript stack (intentional; see ADR-003).
- Microsoft ecosystem gravity is real; mitigated by keeping infrastructure behind interfaces and choosing PostgreSQL (ADR-004) rather than Azure-only data services.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Microsoft-centric hiring later | Domain and API design stay standard HTTP/JSON; frontend is React. |
| LTS end in 2028 | Plan upgrade to the next LTS before end of support. |
| Over-using framework types in domain | Clean Architecture rule: domain does not reference ASP.NET or EF Core. |

---

## Revisit Conditions

- .NET 10 approaches end of support.
- Product direction changes to a runtime this stack cannot serve (not currently indicated).
- Team composition changes such that .NET is no longer maintainable (unlikely in the near term).

---

## Date

2026-08-17

---

## Related Documents

- [ADR-001 Architecture Style](ADR-001-Architecture-Style.md)
- [ADR-005 Data Access](ADR-005-Data-Access.md)
- [ADR-006 API Style](ADR-006-API-Style.md)
- [ADR-007 Authentication Strategy](ADR-007-Authentication-Strategy.md)
- [Engineering Principles](../../engineering/ENGINEERING_PRINCIPLES.md)
