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

- Decisions not yet made
- Trivial implementation choices with no architectural impact
- Product scope decisions (document those in product docs)

---

## ADR Naming Convention

Use sequential numbering with a descriptive slug:

```text
adr/
├── ADR-001-modular-monolith-evaluation.md
├── ADR-002-database-selection.md
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

No ADRs have been created yet. No major architecture decision has been formally frozen.

---

## Related Documents

- [ADR Template](ADR-TEMPLATE.md)
- [Architecture README](../README.md)
- [Engineering Principles](../../engineering/ENGINEERING_PRINCIPLES.md)
