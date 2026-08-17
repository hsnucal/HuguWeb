# HuGuWeb

**HuGuWeb is currently in product discovery and foundation stage.**

HuGuWeb is a hospitality-first ERP / PMS platform being designed to solve real hotel operational problems. The first target industry is **hotels and hospitality**. HuGuWeb is not currently intended to be a generic ERP for every industry.

---

## Current Project Status

| Aspect | Status |
|--------|--------|
| Product discovery | In progress |
| MVP scope | Not yet defined |
| Application code | Not started |
| Architecture decisions | **Accepted** (Sprint 0.3A freeze) — see `docs/architecture/` |
| Technology stack | **Accepted** baseline; remaining items open — see `docs/architecture/TECHNOLOGY_DECISIONS.md` |

No production application functionality exists yet. This repository currently holds product and engineering foundation documentation.

---

## Product Direction

HuGuWeb is being designed around hotel workflows and operational simplicity—not around copying every feature from existing ERP and PMS platforms.

Strategic principle:

> Solve important hotel operational problems better instead of building the largest possible feature list.

Research areas (PMS, reservations, front office, housekeeping, finance, and others) are under investigation. They are **not** approved MVP modules.

---

## Engineering Philosophy

HuGuWeb engineering prioritizes:

- Clean Architecture, SOLID, and Clean Code principles
- High cohesion, low coupling, and explicit boundaries
- Testability, maintainability, and security by design
- Change isolation—a fix in one business area should have minimal impact on unrelated areas
- API-first and cloud-ready thinking without premature complexity

> Architecture is a tool for product delivery, not the product itself.

A **Modular Monolith with Clean Architecture boundaries** is **Accepted** in [ADR-001](docs/architecture/adr/ADR-001-Architecture-Style.md). Final business modules are not defined yet.

---

## Documentation

| Document | Description |
|----------|-------------|
| [Product Vision](docs/product/PRODUCT_VISION.md) | Current product vision and direction |
| [Product Principles](docs/product/PRODUCT_PRINCIPLES.md) | Guiding product principles |
| [Future Scope](docs/product/FUTURE_SCOPE.md) | Documented future product context (not MVP) |
| [Glossary](docs/product/GLOSSARY.md) | Terminology and open definitions |
| [Engineering Principles](docs/engineering/ENGINEERING_PRINCIPLES.md) | Engineering standards and constraints |
| [Development Workflow](docs/engineering/DEVELOPMENT_WORKFLOW.md) | Roles, sprint lifecycle, and collaboration model |
| [Testing Strategy](docs/engineering/TESTING_STRATEGY.md) | Testing philosophy (frameworks not yet selected) |
| [Architecture](docs/architecture/README.md) | Architecture documentation and ADR system |
| [Technology Decisions](docs/architecture/TECHNOLOGY_DECISIONS.md) | Accepted stack and remaining open decisions |
| [Competitor Analysis](docs/research/COMPETITOR_ANALYSIS.md) | Competitor research context |
| [Market Research](docs/research/MARKET_RESEARCH.md) | Market research notes |
| [Roadmap](docs/roadmap/ROADMAP.md) | High-level project phases |

---

## Repository

- **Product working name:** HuGuWeb
- **GitHub repository:** [hsnucal/HuguWeb](https://github.com/hsnucal/HuguWeb)
- **Current development stage:** Sprint 0.3A — Architecture Freeze (**Accepted** ADRs; implementation not authorized)

---

## Collaboration Model

HuGuWeb is developed using a **Product Owner + CTO** decision model. The Product Owner does not automatically dictate implementation; the CTO does not automatically dictate product scope. Both sides are expected to challenge decisions when necessary.

Implementation is assisted by AI coding agents (e.g., Cursor) that follow approved prompts and do **not** make final product or architecture decisions independently.
