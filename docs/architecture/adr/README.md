# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records for HuGuWeb.

---

## Purpose

ADRs document significant architecture decisions with enough context that future team members understand:

- Why a decision was made
- What alternatives were considered
- What consequences and risks follow
- When the decision should be revisited

---

## When to Create an ADR

Create an ADR when a decision has significant and lasting impact, such as:

- Technology stack selection (backend, frontend, database)
- Deployment and hosting model
- Authentication and authorization approach
- Multi-tenancy or multi-property architecture
- Integration architecture patterns
- Adoption of distributed systems, message brokers, or event sourcing
- Module boundary definitions

Do **not** create ADRs for:

- Trivial implementation choices with no architectural impact
- Product scope decisions (document those in product docs)

Proposed ADRs may be written for Product Owner + CTO review. Do not mark them Accepted without that approval.

Sprint 0.3A ADRs listed below were Accepted by Product Owner and CTO on 2026-08-17.

---

## ADR Naming Convention

Use sequential numbering with a descriptive slug:

```text
adr/
├── ADR-001-Architecture-Style.md
├── ADR-002-Backend-Platform.md
└── ...
```

Use the [ADR Template](ADR-TEMPLATE.md) for all new records.

---

## ADR Status Values

| Status | Meaning |
|--------|---------|
| **Proposed** | Under discussion, not yet approved |
| **Accepted** | Approved and in effect |
| **Deprecated** | No longer recommended but may still be in use |
| **Superseded** | Replaced by a newer ADR (link to successor) |

---

## Current ADRs

Sprint 0.3A Architecture Freeze: the following ADRs are **Accepted**. Open decisions (module boundaries, cloud provider, mobile, caching product, brokers, CI/CD, and similar) are recorded in [Technology Decisions](../TECHNOLOGY_DECISIONS.md).

| ADR | Title | Status |
|-----|-------|--------|
| [ADR-001](ADR-001-Architecture-Style.md) | Architecture Style | Accepted |
| [ADR-002](ADR-002-Backend-Platform.md) | Backend Platform | Accepted |
| [ADR-003](ADR-003-Frontend-Architecture.md) | Frontend Architecture | Accepted |
| [ADR-004](ADR-004-Primary-Database.md) | Primary Database | Accepted |
| [ADR-005](ADR-005-Data-Access.md) | Data Access | Accepted |
| [ADR-006](ADR-006-API-Style.md) | API Style | Accepted |
| [ADR-007](ADR-007-Authentication-Strategy.md) | Authentication Strategy | Accepted |
| [ADR-008](ADR-008-Authorization-Strategy.md) | Authorization Strategy | Accepted |
| [ADR-009](ADR-009-Cloud-Strategy.md) | Cloud Strategy | Accepted |
| [ADR-010](ADR-010-Database-Managed-Authorization.md) | Database-Managed Membership Authorization | Accepted |
| [ADR-011](ADR-011-Puantaj-Domain-Model.md) | Puantaj Domain Model | **Accepted** |
| [ADR-012](ADR-012-Workforce-Movements-And-Reporting-Line.md) | Workforce Movements and Reporting Line | **Accepted** |

Summary matrix: [Technology Decisions](../TECHNOLOGY_DECISIONS.md). Product discovery: [HR-07 Puantaj](../../product/hr/HR-07-PUANTAJ-DISCOVERY.md), [HR-08 Personel Hareketleri](../../product/hr/HR-08-PERSONEL-HAREKETLERI-DISCOVERY.md).

---

## Related Documents

- [ADR Template](ADR-TEMPLATE.md)
- [Architecture README](../README.md)
- [Technology Decisions](../TECHNOLOGY_DECISIONS.md)
- [Engineering Principles](../../engineering/ENGINEERING_PRINCIPLES.md)
