# Design Documentation

> **Status:** Sprint 0.4 — Product Experience & Design Foundation. Proposed for Product Owner + CTO review. **Not implementation authorization.**

This directory is the index for HuGuWeb design direction. It does not replace product scope documents. Navigation hypotheses are not approved modules. Visual direction is not a component library.

---

## Sources of Truth

| Document | Owns |
|----------|------|
| [Design Principles](DESIGN_PRINCIPLES.md) | Visual and interaction principles (color use, surfaces, type, density, tables, forms, status, charts, a11y, motion, empty/loading/error, dark mode stance) |
| [UX Architecture](UX_ARCHITECTURE.md) | Navigation, shell, role-aware behavior, onboarding split, future room-board hypothesis |
| [Operations Center](OPERATIONS_CENTER.md) | Authenticated Home experience |
| [Login Experience](LOGIN_EXPERIENCE.md) | Auth entry experience |
| [Design Tokens](DESIGN_TOKENS.md) | Token categories and naming philosophy |
| [Responsive Strategy](RESPONSIVE_STRATEGY.md) | Desktop-first web ERP vs future mobile products |
| [Brand Direction](BRAND_DIRECTION.md) | Personality, purple direction, logo direction, candidate color families |

Cross-reference these documents instead of copying long sections.

Product and architecture context:

- [Product Vision](../product/PRODUCT_VISION.md)
- [Product Principles](../product/PRODUCT_PRINCIPLES.md)
- [Target Customer](../product/TARGET_CUSTOMER.md)
- [Hotel Problems](../product/HOTEL_PROBLEMS.md)
- [MVP Candidates](../product/MVP_CANDIDATES.md)
- [ADR-003 Frontend](../architecture/adr/ADR-003-Frontend-Architecture.md)
- [ADR-007 Authentication](../architecture/adr/ADR-007-Authentication-Strategy.md)
- [ADR-008 Authorization](../architecture/adr/ADR-008-Authorization-Strategy.md)

---

## Design Decision Matrix

Statuses are the Sprint 0.4 freeze for review. They do **not** authorize UI implementation.

| Area | Decision | Status |
|------|----------|--------|
| Brand personality | Warm, hospitality-oriented, modern, calm, long-session friendly, premium-light, professional without cold bureaucracy | **Accepted** |
| Primary color direction | Purple; tokenized so dark green can replace it later | **Accepted** |
| Exact purple palette | Three families documented for comparison | **Candidate** |
| Home = Operations Center | Home answers “what requires my attention right now?” — not a decorative KPI dashboard | **Accepted** |
| React ERP = desktop-first | Tablet-capable; not a phone substitute for the employee mobile product | **Accepted** |
| Dark mode | Do not implement now; tokens must not make it impossible later | **Accepted** (not now) / implementation **Deferred** |
| Navigation philosophy | Work-oriented, permission-aware, calm, no giant ERP tree | **Accepted** |
| Navigation taxonomy | Home / Rooms / Reservations / Tasks / Issues / Search / Settings are hypotheses | **Candidate** |
| Role-aware experience | Home and nav eventually permission-aware; overlapping duties supported | **Accepted** (direction) |
| Role dashboards as products | Not frozen; examples are not feature scope | **Deferred** |
| Employee onboarding vs hotel setup | Different flows; staff join an already configured hotel | **Accepted** |
| Chart philosophy | No chart without a decision purpose; prefer numbers when faster | **Accepted** |
| Generic admin-template avoidance | Explicitly rejected as the visual/UX model | **Accepted** |
| Typography family | Inter / IBM Plex Sans / Source Sans 3 as comparison candidates | **Candidate** |
| Logo design | HG monogram + wordmark direction; no artwork in repo | **Candidate** |
| Icon system | Semantic placeholders only | **Candidate** |
| Component library | None selected | **Deferred** |
| Exact sidebar behavior | Expanded wordmark, collapse later, bottom utility area | **Candidate** |
| Exact top bar composition | Slim bar likely; property / search / alerts as a split vs sidebar | **Candidate** |
| Dashboard density | Comfortable / Standard / Dense; Standard recommended | **Candidate** |
| Table / grid library | None selected | **Deferred** |
| Chart library | None selected | **Deferred** |
| Hotel setup onboarding UI | Future administrator flow only | **Deferred** |
| Operations mobile / employee self-service | Distinct future scopes; technology not selected | **Deferred** |

Exact colors, typography font, icons, and component library are **not** Accepted.

---

## Open Design Decisions

Product Owner + CTO still need to decide:

1. Exact purple family / palette (Families A–C in [Brand Direction](BRAND_DIRECTION.md))
2. Typography family
3. Final logo design (separate review; not to be drawn in this repo now)
4. Icon system
5. Component library strategy
6. Exact sidebar behavior (default expanded vs collapsed, collapse interaction)
7. Exact top bar composition (and where user identity lives)
8. Final default density (Standard is only a recommendation)
9. Table / grid library (when a real dense screen exists)
10. Chart library (only if a decision-purpose chart is approved)

Do not auto-select these during implementation sprints.

---

## Current Bootstrap UI

The Sprint 0.3B React UI is **temporary**. Inspected, not modified, in Sprint 0.4.

Files: `src/frontend/web/src/auth/LoginPage.tsx`, `src/frontend/web/src/app/AppShell.tsx`, `src/frontend/web/src/index.css`, routing in `App.tsx`.

### Remove from the product experience (Sprint 0.5 visual work)

- Unstyled `system-ui` page with default browser form chrome
- “Temporary bootstrap sign-in / Authenticated bootstrap shell” as the standing product surfaces
- Technical status copy as a substitute for Home
- Login and app as a single un-shelled `<main>` with no Operations Center and no work navigation

### Retain technically

- Cookie session flow (`AuthSession`, `sessionApi`, CSRF)
- Email/password sign-in, failure alert, submitting state
- `ProtectedRoute` and `/login` → `/app` routing
- React 19 + Vite + TypeScript foundation ([ADR-003](../architecture/adr/ADR-003-Frontend-Architecture.md))
- Sign-out against the Identity API

### Redesign in Sprint 0.5 (when UI implementation is authorized)

- Login visual per [Login Experience](LOGIN_EXPERIENCE.md)
- Application shell (sidebar + top bar) per [UX Architecture](UX_ARCHITECTURE.md)
- Home as [Operations Center](OPERATIONS_CENTER.md)
- Token-based styling per [Design Tokens](DESIGN_TOKENS.md) — still without freezing unapproved hex/font/library choices
- Empty / loading / error presentation on those two surfaces

Sprint 0.5 must not invent modules, charts, or role dashboards to look finished.

---

## Related Documents

- [Roadmap](../roadmap/ROADMAP.md)
- [README](../../README.md)
