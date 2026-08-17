# ADR-003: Frontend Architecture

## Status

Accepted

Accepted by Product Owner and CTO on 2026-08-17 (Sprint 0.3A Architecture Freeze).

---

## Context

HuGuWeb is a **web-first, authenticated ERP / PMS**. Users are hotel staff and managers, not anonymous public visitors. SEO is **not** a major requirement for the internal application.

The product needs complex operational UI over time: forms, data grids, dashboards, and workflow-oriented screens (“don’t show modules; show work”). A future employee mobile app is documented as future scope, not MVP. The backend is ASP.NET Core API (ADR-002, ADR-006).

Frontend and backend should remain separable so the same API can serve web now and mobile later.

---

## Problem

Should HuGuWeb’s web client be:

- React + Next.js
- a React SPA without Next.js
- or Blazor

Does HuGuWeb actually benefit from Next.js compared with a React SPA?

---

## Decision

We will use a **React 19 + TypeScript + Vite 8 SPA**.

Frozen constraints:

- HuGuWeb is primarily an **authenticated ERP application**.
- **SEO / SSR is not a core requirement** for the internal product.
- **Next.js is not selected.**
- **Blazor is not selected.**

Recommended major versions (verified 2026-08-17; do not pin patches in this ADR):

| Technology | Recommended major | Notes |
|------------|-------------------|--------|
| React | **19** | Current stable line (19.2.x as of July 2026). |
| Vite | **8** | Current stable SPA toolchain (8.2.x as of August 2026). |
| Node.js (tooling only) | **24 LTS** | Active LTS (“Krypton”). EOL 30 April 2028. Not an application runtime for the ERP API. |
| TypeScript | Current stable 5.x at implementation | Patch not pinned here. |

**Explicit answer:** HuGuWeb does **not** meaningfully benefit from Next.js versus a React SPA for the internal ERP application.

---

## Alternatives Considered

| Alternative | Outcome | Reason |
|-------------|---------|--------|
| React SPA + Vite + TypeScript | Accepted | Fits authenticated dashboards, API-first split, simple static deployment, and future mobile via the same API. |
| React + Next.js | Rejected as default | SSR/SSG/SEO and server rendering are not product needs. Adds rendering, caching, and deployment complexity that fights always-fresh operational data. |
| Blazor (Server or WASM) | Rejected as default | C# reuse is real but not decisive. Weaker fit for hiring, complex grid/form ecosystem, and future mobile UI reuse. |
| Mixing Next.js API routes with ASP.NET | Rejected | Would blur API ownership and undermine the modular monolith API. |

### Evaluation against HuGuWeb needs

| Concern | React SPA | Next.js | Blazor |
|---------|-----------|---------|--------|
| ERP dashboard complexity | Strong ecosystem (grids, forms, charts) | Same React ecosystem, plus framework constraints | Capable, smaller component market for dense ERP UI |
| Forms and data grids | Mature React libraries | Same, but data-fetch/cache model is extra surface | Possible; fewer hospitality/ERP UI libraries |
| Routing | Client router is enough for an app behind login | File-based App Router is powerful and heavier than needed | Built-in routing |
| Authentication | Talks to ASP.NET Identity/API; cookies or tokens | Risk of a second auth story (Auth.js / server actions) | Easy with ASP.NET, tighter coupling |
| Frontend/backend separation | Clear: static UI + API | Temptation to put BFF logic in Next | Weaker separation if Blazor Host shares the API process carelessly |
| Mobile future | Same REST API; later React Native or similar can reuse contracts | Same API possible, but Next-specific patterns do not travel | Blazor Hybrid is an option; does not help a likely JS mobile path |
| Hiring / ecosystem | Large | Large, but Next-specific skills | Smaller frontend hiring pool |
| Maintainability | One UI runtime model | Two (server + client components, cache semantics) | One language; Microsoft-shaped UI |
| Rendering needs | Client render after auth is the actual UX | SSR of empty shells behind auth adds little | WASM download or persistent server circuit |
| SEO | Not required for ERP | Primary Next.js strength — unused here | Not relevant |
| Deployment | Static files + API | Node server (or a static export that discards most Next value) | ASP.NET host or static WASM |

### Why Next.js is not justified here

Next.js is a good default for **public, SEO-sensitive, content-heavy** sites and for teams that want React Server Components as the primary data model.

HuGuWeb is the opposite:

- Screens are authenticated operational tools.
- Data must be fresh (room status, arrivals, folio), not statically generated or aggressively cached at the HTTP layer.
- The system of record is the ASP.NET API, not a Next server.
- A separate marketing website, if it ever exists, can use any stack without forcing the ERP shell to follow.

Using Next.js “because it is popular” would add:

- App Router / caching mental load
- a Node production server (or a crippled static export)
- a second place where server logic might accidentally grow

That is premature complexity.

### Why not Blazor as the default

Blazor would let the .NET team share language across UI and API. For HuGuWeb that is not enough:

- Dense ERP UI (grids, filters, operational dashboards) has a deeper React ecosystem.
- Future employee mobile is more likely to share **API contracts** than Blazor components.
- Hiring frontend specialists is easier in React.
- WASM payload and Blazor Server circuit models are extra operational concerns.

Blazor remains a valid *revisit* if the team cannot staff React and the UI stays modest.

---

## Consequences

### Positive

- Clear API-first boundary.
- Static hosting is simple and cloud-portable.
- React skills are widely available.
- No SSR cache invalidation problems for operational data.

### Negative

- Two languages (C# and TypeScript) in the repo — accepted cost.
- Initial HTML is an empty shell; irrelevant for an app that requires login.
- SPA must be designed carefully for auth (see ADR-007): prefer cookies over tokens in `localStorage`.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Accidental “frontend monolith” with no structure | Feature folders aligned to workflows, not ERP module names shown to users. Shared UI kit kept small. |
| Auth token leakage in SPA | Cookie-based auth for web; do not store access tokens in localStorage. |
| Choosing a heavy grid library too early | Start with simple tables; adopt a grid library when a real screen needs it. |
| Vite 8 / React 19 churn | Stay on current stable majors; avoid experimental React Compiler / Next-only features. |

---

## Revisit Conditions

- HuGuWeb gains a public booking or marketing surface where SEO/SSR is actually required (that surface can still be separate).
- Evidence that React staffing is unavailable and Blazor would ship faster with acceptable UX.
- A future mobile strategy that changes the value of shared UI technology (still not a reason to adopt Next.js for the ERP).

---

## Date

2026-08-17

---

## Related Documents

- [ADR-002 Backend Platform](ADR-002-Backend-Platform.md)
- [ADR-006 API Style](ADR-006-API-Style.md)
- [ADR-007 Authentication Strategy](ADR-007-Authentication-Strategy.md)
- [Product Vision](../../product/PRODUCT_VISION.md)
- [Future Scope](../../product/FUTURE_SCOPE.md)
