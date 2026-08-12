# Architecture

This directory contains HuGuWeb architecture documentation and the Architecture Decision Record (ADR) system.

---

## Current Status

**No major architecture decision has been formally approved yet.**

HuGuWeb is in product discovery and foundation stage. Technology stack, deployment model, and module structure are not yet defined.

---

## Architectural Direction (Not Approved)

The following represent **candidates and principles**, not frozen decisions:

| Topic | Status |
|-------|--------|
| Modular Monolith | Strong candidate — not approved |
| Microservices | Avoid unless ADR justifies |
| Multi-tenancy | Not designed — concepts require formal definition |
| Event Sourcing / CQRS | Avoid unless ADR justifies |
| Message brokers / event streaming | Avoid unless ADR justifies |

See [Engineering Principles](../engineering/ENGINEERING_PRINCIPLES.md) for full guidance on premature architecture avoidance.

---

## Change Safety

A first-class engineering requirement:

> A change or bug fix in one business area should have the smallest reasonable impact on unrelated business areas.

Future architecture should encourage module isolation, explicit contracts, dependency boundaries, and controlled database evolution. The actual module architecture is **not designed yet**.

---

## Multi-Property Consideration

Hotel chains and multi-property management are strategically relevant. Future architecture decisions should evaluate multi-property requirements early enough to avoid expensive redesign.

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

Do not create ADRs for decisions that have not been formally made.

---

## Technology Decisions Deferred

All technology selections are open. See [Engineering Principles — Technology Decisions Explicitly Deferred](../engineering/ENGINEERING_PRINCIPLES.md#technology-decisions-explicitly-deferred).

---

## Related Documents

- [Engineering Principles](../engineering/ENGINEERING_PRINCIPLES.md)
- [Testing Strategy](../engineering/TESTING_STRATEGY.md)
- [Product Vision](../product/PRODUCT_VISION.md)
- [Future Scope](../product/FUTURE_SCOPE.md)
