# ADR-009: Cloud Strategy

## Status

Accepted

Accepted by Product Owner and CTO on 2026-08-17 (Sprint 0.3A Architecture Freeze).

This ADR accepts a **provider-neutral initial architecture**. It does **not** select Azure, AWS, or any other cloud vendor.

---

## Context

HuGuWeb must be cloud-ready without premature cloud-native complexity. No production workload exists. Kubernetes, multi-region, and vendor-specific PaaS suites are not current problems.

Azure and AWS are both capable. There is no overwhelmingly strong product reason to lock the foundation to one vendor in this sprint.

The goal is **practical portability**, not theoretical independence at any cost (no lowest-common-denominator wrappers that make managed services unusable).

---

## Problem

How cloud-specific should the initial architecture be, and should HuGuWeb choose Azure or AWS now?

---

## Decision

We will use a **provider-neutral initial design**.

Provider-neutral does **not** mean creating generic abstractions around every cloud feature. Avoid speculative portability layers. No Azure or AWS provider is selected yet.

Frozen constraints:

- Application: ASP.NET Core API + React static SPA + PostgreSQL (see related ADRs)
- Prefer **open or portable building blocks** (PostgreSQL, object storage *API* later, OpenTelemetry)
- When a cloud is eventually chosen, use that cloud’s **managed PostgreSQL, secrets, and hosting** directly — through thin infrastructure adapters, not a custom “cloud abstraction layer”
- **Do not select Azure or AWS in this ADR**

Hosting at bootstrap (when implementation is authorized) is a simple topology:

```text
Web Frontend (static SPA)
      ↓
ASP.NET Core API
      ↓
PostgreSQL
```

That topology can run on a VM, App Service, ECS, Cloud Run, or a developer machine. The architecture must not assume Kubernetes.

---

## Alternatives Considered

| Alternative | Outcome | Reason |
|-------------|---------|--------|
| Provider-neutral app + PostgreSQL | Accepted | Matches “no vendor yet,” keeps local dev simple, and still deploys cleanly to Azure or AWS later. Avoids speculative portability frameworks. |
| Azure-first (App Service, Azure SQL, Entra) | Rejected as a *foundation lock* | Azure is a strong later candidate given .NET, but Azure SQL + Entra + Service Bus as defaults would pre-decide database, identity, and messaging. |
| AWS-first (ECS, RDS, Cognito) | Rejected as a *foundation lock* | Equally capable; no current operational or commercial evidence. Cognito would fight ADR-007. |
| Kubernetes from day one | Rejected | No scale, no multi-service fleet, no platform team. |
| Heavy “cloud abstraction” library | Rejected | Makes real managed services harder; violates “architecture is a tool.” |

---

## Consequences

### Positive

- CTO/PO can pick a cloud when there is a real hosting, commercial, or customer reason.
- Local development does not require a cloud account.
- Choosing PostgreSQL (ADR-004) and in-app Identity (ADR-007) avoids the two most common accidental lock-ins.

### Negative

- We will not get maximum Azure-native or AWS-native convenience on day one (e.g. we will not start on Cosmos DB or DynamoDB).
- Some later managed features (queues, identity federation) will be adopted explicitly via new ADRs.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Accidental lock-in via SDK sprawl | Keep vendor SDKs in infrastructure adapters. Domain never imports cloud SDKs. |
| Premature multi-cloud | Portability ≠ running on two clouds at once. Pick one when deploying production. |
| Reinventing cloud services | When Redis, object storage, or email is actually needed, use the chosen cloud’s managed offering (or a portable equivalent) — do not write a generic framework first. |

---

## Revisit Conditions

- First production/pilot hosting decision is made (then record the vendor in a successor ADR).
- A customer contract mandates a specific cloud.
- A managed service (email, object storage, secrets) is required and the vendor choice becomes load-bearing.

---

## Date

2026-08-17

---

## Related Documents

- [ADR-001 Architecture Style](ADR-001-Architecture-Style.md)
- [ADR-002 Backend Platform](ADR-002-Backend-Platform.md)
- [ADR-003 Frontend Architecture](ADR-003-Frontend-Architecture.md)
- [ADR-004 Primary Database](ADR-004-Primary-Database.md)
- [ADR-007 Authentication Strategy](ADR-007-Authentication-Strategy.md)
- [Engineering Principles](../../engineering/ENGINEERING_PRINCIPLES.md)
