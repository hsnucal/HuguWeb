# HuGuWeb

**HuGuWeb is currently in product discovery and foundation stage.**

HuGuWeb is a hospitality-first ERP / PMS platform being designed to solve real hotel operational problems. The first target industry is **hotels and hospitality**. HuGuWeb is not currently intended to be a generic ERP for every industry.

---

## Current Project Status

| Aspect | Status |
|--------|--------|
| Product discovery | In progress |
| MVP scope | Not yet defined |
| Application foundation | **Sprint 0.3B bootstrap** (API + SPA + Identity; no hotel domain features) |
| Product experience / design | **Sprint 0.5A UI** plus **Sprint 0.6 localization** (uncommitted until review) |
| Localization | **Turkish, English, Russian** with persisted user language preference — see `docs/product/LOCALIZATION.md` |
| Architecture decisions | **Accepted** (Sprint 0.3A freeze) — see `docs/architecture/` |
| Technology stack | **Accepted** baseline; remaining items open — see `docs/architecture/TECHNOLOGY_DECISIONS.md` |

Hotel operational functionality is not implemented. The repository now contains a lean application foundation that can build, run, and test.

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

A **Modular Monolith with Clean Architecture boundaries** is **Accepted** in [ADR-001](docs/architecture/adr/ADR-001-Architecture-Style.md). Final business modules are not defined yet. Empty module projects are not created until approved functionality exists.

---

## Repository Structure

```text
/
├── src/
│   ├── backend/HuGuWeb.Api/
│   └── frontend/web/
├── tests/
│   ├── HuGuWeb.ArchitectureTests/
│   └── HuGuWeb.UnitTests/
├── docs/
├── HuGuWeb.slnx
└── README.md
```

---

## Local Development

Preferred daily workflow in Cursor / VS Code:

1. Open the repository root.
2. Press **F5**.
3. Select **HuGuWeb Development**.

That sequence checks PostgreSQL with `pg_isready` on `127.0.0.1:5432` (and starts or gracefully restarts the existing development cluster only if it is not accepting connections), starts Vite if needed and waits until `http://localhost:5173` returns 200, builds the API, starts the API under the debugger, waits for `/health`, then opens Chrome.

| Surface | Address | Notes |
| --- | --- | --- |
| Frontend (Chrome) | http://localhost:5173 | Login page. This is the F5 browser target. |
| API | http://localhost:5116 | Debugger; `/health` and `/health/ready`. Not opened in Chrome. |
| PostgreSQL | localhost:5432 | Database port. **Not an HTTP URL** — do not open it in Chrome. |

CLI fallback from the repository root on Windows:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\dev.ps1
```

`Bypass` applies only to that process. Do not run `Set-ExecutionPolicy`. Do not open `http://localhost:5432` in Chrome.

Prerequisites, stopping behavior, and manual fallback commands: [Local Development](docs/engineering/LOCAL_DEVELOPMENT.md).

Do not commit secrets. Use user secrets or environment variables for the development user password and any real connection string.

Tests and builds:

```bash
dotnet test
cd src/frontend/web
npm run lint
npm run build
```

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
| [Testing Strategy](docs/engineering/TESTING_STRATEGY.md) | Testing philosophy |
| [Local Development](docs/engineering/LOCAL_DEVELOPMENT.md) | Bootstrap runbook (config, secrets, database, commands) |
| [Design](docs/design/README.md) | Product experience and design foundation (Sprint 0.4) |
| [Localization](docs/product/LOCALIZATION.md) | Supported languages, fallback, and user language persistence |
| [Architecture](docs/architecture/README.md) | Architecture documentation and ADR system |
| [Technology Decisions](docs/architecture/TECHNOLOGY_DECISIONS.md) | Accepted stack and remaining open decisions |
| [Competitor Analysis](docs/research/COMPETITOR_ANALYSIS.md) | Competitor research context |
| [Market Research](docs/research/MARKET_RESEARCH.md) | Market research notes |
| [Roadmap](docs/roadmap/ROADMAP.md) | High-level project phases |

---

## Repository

- **Product working name:** HuGuWeb
- **GitHub repository:** [hsnucal/HuguWeb](https://github.com/hsnucal/HuguWeb)
- **Current development stage:** Sprint 0.6 — Localization & User Preferences Foundation (uncommitted until review)

---

## Collaboration Model

HuGuWeb is developed using a **Product Owner + CTO** decision model. The Product Owner does not automatically dictate implementation; the CTO does not automatically dictate product scope. Both sides are expected to challenge decisions when necessary.

Implementation is assisted by AI coding agents (e.g., Cursor) that follow approved prompts and do **not** make final product or architecture decisions independently.
