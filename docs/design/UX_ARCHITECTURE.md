# UX Architecture

> **Status:** Sprint 0.4 design freeze — proposed for Product Owner + CTO review. Navigation, shell, and role *behavior* only. Not approved module scope. Not an information architecture freeze.

This is the source of truth for **navigation, application shell, role-aware experience, and onboarding split**.

Related sources of truth:

| Topic | Document |
|-------|----------|
| Visual / interaction rules | [Design Principles](DESIGN_PRINCIPLES.md) |
| Home structure | [Operations Center](OPERATIONS_CENTER.md) |
| Sign-in | [Login Experience](LOGIN_EXPERIENCE.md) |
| Desktop vs tablet vs mobile product | [Responsive Strategy](RESPONSIVE_STRATEGY.md) |
| Permission model (technical) | [ADR-008](../architecture/adr/ADR-008-Authorization-Strategy.md) |

---

## Core UX Principle

> **Don't show modules. Show work.**

Users should think about arrivals, departures, rooms, tasks, requests, issues, and approvals—not about HuGuWeb’s internal module map.

This is a **UX principle**. Technical module boundaries still exist in the architecture ([ADR-001](../architecture/adr/ADR-001-Architecture-Style.md)). The UI must not force staff to learn those boundaries to do their jobs.

Do not treat the navigation hypotheses below as approved PMS modules. Capability candidacy remains in [MVP Candidates](../product/MVP_CANDIDATES.md).

---

## Navigation Philosophy

Navigation should be:

- work-oriented
- permission-aware
- predictable
- visually calm
- fast to scan

Avoid a giant ERP menu tree. Avoid nested accordions of module names.

### UI hypotheses (not approved taxonomy)

These labels are **navigation hypotheses** for desktop ERP exploration. They are not a product backlog:

- Home
- Rooms
- Reservations
- Tasks
- Issues / Maintenance
- Search
- Settings

Do **not** create a final navigation taxonomy in this sprint. Do not add Finance, Inventory, HR, or similar items to the primary nav just because ERP products usually have them.

Search may be a top-bar control rather than a primary nav row. That composition is still open.

---

## Application Shell

Desktop ERP uses a stable shell: sidebar + optional slim top bar + work area.

Desktop sidebar collapse is **accepted** ([Responsive Strategy](RESPONSIVE_STRATEGY.md)). Exact top-bar composition remains a **candidate** (see [Open Decisions](README.md#open-design-decisions)).

### Sidebar direction

Requirements:

- expanded state shows the HG mark + HuGu
- collapsed state shows the HG mark alone (when that rail exists)
- the HG/HuGu brand area is the collapse/expand control; there is no standalone chevron
- current selection clearly visible (brand accent + label, not color alone)
- no excessive nested menus
- bottom area may contain help, settings, and user context
- permission-hidden items must not leave confusing gaps — collapse the list; do not show disabled ghosts for every hidden area

Do **not** design final icons. Use semantic placeholders only (`Home`, `Rooms`, `Tasks`).

Expanded-state hypothesis (low fidelity):

```text
┌──────────────────────┐
│  HuGuWeb             │
├──────────────────────┤
│  (H)  Home           │  ← current
│  (R)  Rooms          │
│  (B)  Reservations   │
│  (T)  Tasks          │
│  (I)  Issues         │
├──────────────────────┤
│  (?)  Help           │
│  (G)  Settings       │
│  ————————            │
│  A. Yılmaz           │
│  Front office        │
└──────────────────────┘
```

Collapsed-state (desktop rail):

```text
┌────┐
│ HG │
├────┤
│ H  │
│ R  │
│ B  │
│ T  │
│ I  │
├────┤
│ ?  │
│ G  │
│ AY │
└────┘
```

Collapsed labels must remain available via accessible names / tooltips. Icon-only navigation is not sufficient by itself.

### Sidebar vs top bar

Do not put the same control in both places.

| Concern | Default home (candidate) |
|---------|--------------------------|
| Primary destinations | Sidebar |
| Help / settings / signed-in identity | Sidebar bottom |
| Current property / context | Top bar |
| Global search | Top bar |
| Notifications | Top bar |
| Operational date / “today” | Top bar, only if it helps the current work |
| Primary actions for the current screen | Work area, not chrome |

### Top bar direction

A persistent slim top bar is **likely useful** so property context, search, and notifications stay available while the sidebar stays dedicated to destinations.

It must not become a second application. Avoid packing KPIs, filters, and module switchers into the top bar.

Candidate composition:

```text
[ Property: Demo Hotel ]   [ Search rooms, guests, tasks… ]   [ 17 Aug ]   [ Alerts ]   [ optional: overflow ]
```

User profile may live in the sidebar bottom **or** the top bar—not both as competing entry points. Recommendation for review: identity in the sidebar; alerts in the top bar.

Single-property pilots can show a static property context. That slot should exist so a later property switch does not invent a new chrome pattern. Multi-property switching is **not** in scope ([Technology Decisions](../architecture/TECHNOLOGY_DECISIONS.md)).

---

## Home

The authenticated Home screen is an **Operations Center**.

It answers: **What requires my attention right now?**

Structure, wireframe, and anti-patterns: [Operations Center](OPERATIONS_CENTER.md).

---

## Role-Aware Experience

HuGuWeb should eventually support role- and permission-aware Home and navigation ([ADR-008](../architecture/adr/ADR-008-Authorization-Strategy.md): enforce **permissions**, not hard-coded department names).

This is **UX direction**. It is not an approved set of role dashboards. Do not implement per-role Home products in this sprint. Do not freeze the examples below as feature scope.

Illustrative attention sets:

### Front Office

May care about arrivals, departures, rooms not ready, guest issues, room changes.

### Housekeeping Supervisor

May care about dirty rooms, cleaning progress, inspection queue, rework, urgent rooms.

### Technical Service

May care about open faults, out-of-order / out-of-service rooms, priority issues.

### Management

May care about operational bottlenecks, unresolved critical issues, department workload, and KPIs **only when they change a decision**.

The same Operations Center layout can emphasize different attention lists. Do not fork four unrelated dashboard products.

Users in independent mid-size hotels may wear multiple hats ([Target Customer](../product/TARGET_CUSTOMER.md)). The shell must tolerate overlapping permissions without showing four competing Homes.

---

## Onboarding Strategy

Employee access onboarding and hotel setup onboarding are **different product flows**.

### Employee / staff access

Staff users should **not** go through generic SaaS company-creation onboarding.

They should normally receive access to an **already configured hotel**, then sign in ([Login Experience](LOGIN_EXPERIENCE.md)) and land in the Operations Center (or a permission-appropriate first screen).

Invitations, disable-on-turnover, and password reset are authentication product concerns ([ADR-007](../architecture/adr/ADR-007-Authentication-Strategy.md)). They are not a marketing onboarding wizard.

### Hotel setup / administrator onboarding

Future administrator setup may include hotel information, property structure, departments, staff, permissions, and integrations.

This is **not** being implemented now. It must not be bolted onto the staff login screen.

---

## Room Operations — Future Visual Hypothesis

Not a Sprint 0.4 implementation requirement. Not a domain status model.

A future room-operations view may group by floor and show compact room cards with:

- room identity
- status
- assignment
- arrival context
- important notes
- blockers

```text
Floor 2
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│ 201         │ │ 202         │ │ 203         │
│ [status]    │ │ [status]    │ │ [status]    │
│ Assigned: — │ │ Assigned: AY│ │ Blocker: AC │
│ Arr. 15:00  │ │ Vacant      │ │ Arr. 18:00  │
└─────────────┘ └─────────────┘ └─────────────┘
```

Do **not** define final Room domain statuses here. Visual language for status chips is in [Design Principles](DESIGN_PRINCIPLES.md).

---

## Permission-Hidden Navigation

If the user cannot use a destination:

- **hide it** when it is not part of their work
- do not leave empty numbered holes in the sidebar
- do not show a locked forest of modules “for discoverability”
- if a deep link is unauthorized, explain that access is missing and offer a safe next step (usually Home)

Settings that the user cannot change should not appear as broken rows.

---

## Related Documents

- [Operations Center](OPERATIONS_CENTER.md)
- [Login Experience](LOGIN_EXPERIENCE.md)
- [Responsive Strategy](RESPONSIVE_STRATEGY.md)
- [Product Principles](../product/PRODUCT_PRINCIPLES.md)
- [ADR-003](../architecture/adr/ADR-003-Frontend-Architecture.md)
- [ADR-007](../architecture/adr/ADR-007-Authentication-Strategy.md)
- [ADR-008](../architecture/adr/ADR-008-Authorization-Strategy.md)
