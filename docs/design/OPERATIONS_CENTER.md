# Operations Center

> **Status:** Sprint 0.4 design freeze — proposed for Product Owner + CTO review. Home experience direction and low-fidelity wireframe only. Examples are UX hypotheses, not approved MVP widgets.

This is the source of truth for the **authenticated Home screen**.

Shell, navigation, and role behavior: [UX Architecture](UX_ARCHITECTURE.md).  
Chart and density rules: [Design Principles](DESIGN_PRINCIPLES.md).

---

## Freeze

> **The authenticated Home screen is an Operations Center.**

It answers:

> What requires my attention right now?

It is **not** primarily a traditional KPI dashboard. It is **not** a collection of decorative charts.

---

## What Home Prioritizes

| Priority | Why it is on Home |
|----------|-------------------|
| Actionable issues | Someone can do something now |
| Operational delays | Time is slipping (room not ready, inspection waiting) |
| Approaching deadlines | Arrivals, cut-off times, promised times |
| Arrivals / departures | Today’s guest movement |
| Room readiness | Blocks check-in |
| Pending approvals / tasks | Work parked on this role |
| Role-relevant alerts | Permission-aware; not a global firehose |

Use KPIs only when they help the user decide what to do. A count that cannot be opened into work does not belong as a hero tile.

Do not fill remaining space with finance charts, occupancy donuts, or generic ERP metrics.

---

## Structure

Recommended regions, in scan order:

1. **Today** — compact operational counts for the current date
2. **Requires attention** — the primary work list
3. **Room operations snapshot** — compact state summary
4. **Upcoming operational events** — only if it changes action

Regions may hide when empty or when the user’s permissions make them irrelevant. Do not show locked placeholder charts.

Role-aware emphasis is direction only; see [UX Architecture](UX_ARCHITECTURE.md). Do not implement separate dashboard products per department.

---

## Low-Fidelity Wireframe

Desktop hypothesis. Labels in parentheses are semantic placeholders, not icons.

```text
┌─────────────┬──────────────────────────────────────────────────────────────────┐
│ HuGuWeb     │  Property: (name)    [ Search ]    (date)    (alerts)            │
├─────────────┼──────────────────────────────────────────────────────────────────┤
│ (H) Home  ● │  Operations Center                                               │
│ (R) Rooms   │                                                                  │
│ (B) Reserv. │  TODAY                                                           │
│ (T) Tasks   │  ┌────────────┐  ┌────────────┐  ┌─────────────────┐             │
│ (I) Issues  │  │ Arrivals   │  │ Departures │  │ Rooms not ready │             │
│             │  │ 12         │  │ 9          │  │ 4               │             │
│             │  │ peak 14:00 │  │ until 11:00│  │ 2 due before    │             │
│             │  └────────────┘  └────────────┘  │    next arrival │             │
│             │                                  └─────────────────┘             │
│ (Help)      │                                                                  │
│ (Settings)  │  REQUIRES ATTENTION                                              │
│ A. Yılmaz   │  !  Room 214 not ready · arrival 15:00 · cleaning delayed        │
│             │  !  Room 308 out of order · open issue 6h · technical service    │
│             │  !  Inspection waiting · 3 rooms · supervisor queue              │
│             │  o  Late checkout request · 412 · needs approval                 │
│             │                                                                  │
│             │  ROOM OPERATIONS SNAPSHOT                                        │
│             │  Ready 42   Cleaning 11   Inspection 3   Blocked 2   Occupied 68 │
│             │                                                                  │
│             │  UPCOMING                                                        │
│             │  16:00  Group arrival · 18 rooms                                 │
│             │  18:00  VIP arrival · 501                                        │
└─────────────┴──────────────────────────────────────────────────────────────────┘
```

Notes on this wireframe:

- Today tiles are **small and factual**, not oversized dashboard cards.
- Requires attention is a **list of work**, each row opening a workflow—not a chart.
- Snapshot is a **single compact line** (or equivalent), not five donuts.
- Upcoming is optional. Omit it when it adds noise.
- Counts and room numbers are **examples for layout**, not sample product data to implement.

---

## Today

Purpose: orient the user to the operating day.

Example contents (hypotheses):

- arrivals
- departures
- rooms not ready

Each item should be tappable/clickable into the relevant work, not a dead statistic.

Do not add RevPAR, ADR, or monthly revenue here by default.

---

## Requires Attention

Purpose: the main decision list.

Example contents (hypotheses):

- room not ready before arrival
- unresolved maintenance
- supervisor inspection waiting
- operational blockers

Each row should say:

- what is wrong
- where (room / guest / request)
- why it matters now (time, guest impact, blocker)
- who it is waiting on, when known

Severity should use status language from [Design Principles](DESIGN_PRINCIPLES.md) (label + semantic color + not color-only).

Empty state: explain that nothing needs attention, and point to Today or Rooms if useful.

---

## Room Operations Snapshot

Purpose: one glance at house state.

This is a compact summary, not a room board. A future floor/board view is a hypothesis in [UX Architecture](UX_ARCHITECTURE.md).

Do not freeze Room domain statuses here. The wireframe uses plain-language buckets only as layout examples.

---

## Upcoming Operational Events

Include only when it helps the next action (group arrival, VIP, scheduled outage).

Do not build a full calendar product on Home.

---

## Charts on Home

Default: **none**.

A chart may appear on Home only if it answers a decision the current user must make in this session (for example, aging of unresolved issues for a manager). Even then, prefer a number plus a link to the work list.

[No chart without a decision purpose](DESIGN_PRINCIPLES.md#data-visualization).

---

## What Home Is Not

- a BI workspace
- a module launcher
- a marketing dashboard
- a per-department product to be scoped and built as four apps
- a place to preview every ERP capability the hotel might buy later

---

## Related Documents

- [UX Architecture](UX_ARCHITECTURE.md)
- [Design Principles](DESIGN_PRINCIPLES.md)
- [Hotel Problems](../product/HOTEL_PROBLEMS.md)
- [MVP Candidates](../product/MVP_CANDIDATES.md)
