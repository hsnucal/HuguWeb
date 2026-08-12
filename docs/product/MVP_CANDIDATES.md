# MVP Candidates

> **Important:** This document does **not** define the MVP. It records current capability groupings for Product Owner + CTO review.

All items are **candidates**, not approved scope. Final MVP scope requires explicit approval in a future approved scope document (see [Roadmap](../roadmap/ROADMAP.md) Phase 1).

---

## Purpose

Sprint 0.2 consolidates research into candidate capability groups to support discovery—not to freeze implementation scope.

**Discovery segment selected; MVP classifications unchanged.** Independent Mid-Size is the approved discovery/pilot focus ([Target Customer](TARGET_CUSTOMER.md)). That selection does **not** promote Inventory, Purchasing, Finance, Accounting, Maintenance, POS, HR, Employee Mobile, Booking Engine, Channel Manager, or multi-property UI into MVP. Those decisions still require workflow discovery and additional evidence.

Each item below states:

- Why it may matter
- Justification basis (operational dependency vs market signal vs competitor parity)
- What evidence is missing
- Key risks
- Current recommendation status

---

## How “Strong” is justified

**Strong** requires an **operational dependency** for running a hotel stay (A), optionally supported by market/research signal (B).

Competitor feature parity alone (C) is **not** sufficient. Current evidence is **E0–E1** only.

---

## Strong MVP Candidates

Capabilities that currently appear **operationally required to run a guest stay**, **if** a PMS-centric entry point is validated for the target segment.

### Reservation

| Aspect | Detail |
|--------|--------|
| Why it may matter | A hotel cannot sell or honor occupancy without a booking/availability record. Downstream arrivals, rooms, and housekeeping depend on it. |
| Justification basis | **A — operational dependency.** Not “PMS products usually have reservations.” |
| Missing evidence | Target segment channel mix; required OTA/channel depth on day one |
| Key risks | Over-scoping channel parity; treating channel manager as an internal build |
| Current status | **Strong MVP candidate** — pending validation of operational assumptions for the Independent Mid-Size discovery segment |

### Front Office

| Aspect | Detail |
|--------|--------|
| Why it may matter | Arrival, in-stay handling, and departure are the daily operating hub. Check-in cannot complete without room assignment and readiness. |
| Justification basis | **A — operational dependency.** |
| Missing evidence | Specific check-in/out pain points; identity/document workflow requirements by market |
| Key risks | Feature creep into full CRM or payment processing |
| Current status | **Strong MVP candidate** — pending validation of operational assumptions for the Independent Mid-Size discovery segment |

### Room Management

| Aspect | Detail |
|--------|--------|
| Why it may matter | Sellable units, types, blocks, and occupancy/status are the inventory the front office assigns. Without this, reservation and housekeeping have nothing to coordinate against. |
| Justification basis | **A — operational dependency.** |
| Missing evidence | Complex room hierarchy needs (connecting rooms, multi-building resorts) |
| Key risks | Resort-specific complexity too early for pilot |
| Current status | **Strong MVP candidate** — pending validation of operational assumptions for the Independent Mid-Size discovery segment |

### Housekeeping

| Aspect | Detail |
|--------|--------|
| Why it may matter | Front office cannot reliably check guests in without room-readiness status. That coordination is an operating dependency, not a nice-to-have module. |
| Justification basis | **A — operational dependency** for **room-readiness coordination with front office.** Not a full housekeeping operations platform (task boards, inspections, linen programs). |
| Missing evidence | How status is updated today (paper, radio, WhatsApp, PMS); supervisor inspection depth; whether assignment workflow is first-pilot value |
| Key risks | Expanding into a full housekeeping product; assuming mobile app scope (employee mobile remains [Future Scope](FUTURE_SCOPE.md)) |
| Current status | **Strong MVP candidate** for room-readiness coordination only — pending validation of workflow depth |

### Folio / Guest Charges

| Aspect | Detail |
|--------|--------|
| Why it may matter | Checkout requires a running record of what the guest owes. That is an operational financial event, not statutory accounting. |
| Justification basis | **A — operational dependency** for stay settlement. |
| Missing evidence | Payment integration requirements; POS charge posting needs |
| Key risks | Treating folio as full accounting; building a payment gateway |
| Current status | **Strong MVP candidate** (operational folio / guest charges only — **not** finance visibility, accounting integration, or full statutory accounting) |

---

## Conditional MVP Candidates

Areas that **may** be valuable early but require **target-segment validation** before inclusion in any MVP proposal.

### Guest identity vs Guest Profiles

| Aspect | Detail |
|--------|--------|
| Why it may matter | Reservation, stay, and folio need *some* guest identity (name, contact). That is not the same as a Guest Profiles capability (searchable history, preferences, repeat-guest records). |
| Justification basis | Identity data is an operational necessity **attached to stay workflows**. A distinct profiles capability is currently **E0/E1** and partly competitor-pattern (C) — not enough for Strong. |
| Missing evidence | Required profile depth; whether staff actually search/reuse profiles daily; privacy and retention requirements |
| Key risks | Building advanced CRM before core operations are validated; inflating “guest” into a module |
| Current status | **Conditional MVP candidate** — minimal identity implied by reservation/folio; distinct Guest Profiles **Needs More Research** |

### Inventory

| Aspect | Detail |
|--------|--------|
| Why it may matter | *May* support housekeeping, F&B, and maintenance consumption tracking; ERP differentiation **hypothesis** (E0) |
| Missing evidence | Whether the pilot segment manages stock in-system vs spreadsheets; whether first-pilot value exists at all; required depth |
| Key risks | High implementation scope for small/boutique; weak adoption if too heavy; **scope trap** if treated as core PMS |
| Current status | **Conditional MVP candidate** — **not** assumed required for first-pilot value. Default lean **Next** / **Needs More Research**. Selecting Independent Mid-Size as the discovery segment does **not** promote Inventory into MVP |

### Purchasing

| Aspect | Detail |
|--------|--------|
| Why it may matter | *May* close requisition → approval → receiving loops described as hypotheses in [Hotel Problems](HOTEL_PROBLEMS.md) |
| Missing evidence | Whether purchasing pain is material in hotel operations (vs generic ERP); approval complexity; existing ERP habits |
| Key risks | Competing with entrenched ERP workflows; long implementation tail; **scope trap** if included to “look like an ERP” |
| Current status | **Conditional MVP candidate** — **not** assumed required for first-pilot value. Default lean **Next** / **Needs More Research** |

### Open decision: Inventory and Purchasing

**Product Owner + CTO must decide. This document does not.**

These capabilities sit in tension:

| Question | Current position |
|----------|------------------|
| Required for first-pilot value? | **Not assumed.** Unknown; likely segment-dependent |
| Differentiator or scope trap? | **Both are possible.** Connected back-office *may* differentiate vs PMS-only tools; early depth *may* explode scope |
| Does importance change by segment? | **Likely yes.** Small/boutique: higher overkill risk. Mid-size: higher process complexity (hypothesis). Resort: F&B/maintenance stock may matter more (hypothesis) |

Do not promote Inventory or Purchasing into Strong/MVP on competitor ERP parity.

---

### Reporting / Management Dashboard

| Aspect | Detail |
|--------|--------|
| Why it may matter | Supports GM visibility hypothesis; aligns with workflow-oriented UX principle |
| Missing evidence | Which metrics matter daily vs monthly; existing reporting workarounds |
| Key risks | Building BI before operational data exists; dashboard without actionable workflows |
| Current status | **Conditional MVP candidate** — minimal operational reporting possible early |

### Finance (Operational Hooks)

| Aspect | Detail |
|--------|--------|
| Why it may matter | Operations generate financial *events* (charges, deposits, settlements). That is not the same as running the hotel’s books. |
| Missing evidence | Which of the layers below the pilot actually needs inside HuGuWeb vs in an external accounting system |
| Key risks | Scope creep into full accounting; HuGuWeb accidentally becoming an accounting product |
| Current status | **Conditional MVP candidate** — scope boundary **Needs More Research** |

Financial layers are **not** the same product scope (none decided here):

| Layer | Meaning | Current lean |
|-------|---------|--------------|
| Operational financial events | Charge posted, discount, deposit | Near folio / front office |
| Folio / guest charges | Running guest account during stay | Strong MVP candidate (operational) |
| Payment handling | Capture, refund, pre-auth | Likely **integrate** |
| Finance visibility | What is outstanding / posted today | Conditional; not accounting |
| Accounting integration | Export or post to an external GL | Likely **integrate** |
| Full statutory accounting | GL, AP/AR close, tax books | **Not** automatic HuGuWeb scope; likely integrate or later |

### Maintenance (Basic Work Orders)

| Aspect | Detail |
|--------|--------|
| Why it may matter | Room defects affect housekeeping and front office; out-of-order coordination |
| Missing evidence | Whether pilot properties track maintenance digitally today |
| Key risks | Asset/PM depth expands quickly (IFS-like scope) |
| Current status | **Conditional MVP candidate** — default **Next** |

### Channel / OTA Connectivity (via Integration)

| Aspect | Detail |
|--------|--------|
| Why it may matter | Reservations may require external distribution connectivity |
| Missing evidence | Minimum integration set for pilot; build vs partner strategy |
| Key risks | Blocking pilot on full channel parity |
| Current status | **Conditional MVP candidate** as **integration dependency**, not internal build |

---

## Explicitly Not MVP / Likely Later

Capabilities currently expected to remain **later**, **integration-oriented**, or **future scope** unless discovery strongly contradicts.

### Employee Mobile App

| Aspect | Detail |
|--------|--------|
| Why it may matter later | Floor staff (housekeeping, maintenance) may need mobile workflows |
| Missing evidence | Whether web-responsive workflows suffice initially |
| Key risks | Premature mobile platform selection and scope explosion |
| Current status | **Explicitly not MVP** — see [Future Scope](FUTURE_SCOPE.md) |

### Own Channel Manager

| Aspect | Detail |
|--------|--------|
| Why it may matter later | Only if integration strategy fails commercially or technically |
| Missing evidence | Partner/integration availability in target market |
| Key risks | High maintenance; low differentiation |
| Current status | **Likely Integrate, not MVP build** |

### Own Revenue Management Engine

| Aspect | Detail |
|--------|--------|
| Why it may matter later | Revenue optimization for sophisticated commercial teams |
| Missing evidence | Pilot segment RM maturity and tooling |
| Key risks | Specialized domain; long time to value |
| Current status | **Likely Integrate or later** |

### Advanced CRM

| Aspect | Detail |
|--------|--------|
| Why it may matter later | Sales pipelines, campaigns, loyalty |
| Missing evidence | Whether operational guest profiles satisfy early needs |
| Key risks | Distraction from operational core |
| Current status | **Likely later or integrate** |

### Complex Multi-property UI

| Aspect | Detail |
|--------|--------|
| Why it may matter later | Hotel groups need portfolio views and permissions |
| Missing evidence | Tenant/Property/Group domain model |
| Key risks | Architecture and UX complexity before single-property validation |
| Current status | **Explicitly not MVP** — see [Future Scope](FUTURE_SCOPE.md) |

### Full Accounting / GL

| Aspect | Detail |
|--------|--------|
| Why it may matter later | Complete financial close and statutory reporting |
| Missing evidence | Integrate vs build boundary; local ERP coexistence model |
| Key risks | Regulatory and maintenance burden; becoming an accounting software company |
| Current status | **Likely Integrate or later** — HuGuWeb must **not** automatically become an accounting product |

### Full HR / Payroll

| Aspect | Detail |
|--------|--------|
| Why it may matter later | Scheduling, leave, payroll, staff services |
| Missing evidence | Pilot segment HR tooling; payroll integration requirements |
| Key risks | Compliance and breadth of HR domains |
| Current status | **Likely Integrate or later** |

### POS / F&B Platform

| Aspect | Detail |
|--------|--------|
| Why it may matter later | Only if deep F&B integration is strategic for HuGuWeb |
| Missing evidence | F&B revenue share and POS vendor landscape at pilot properties |
| Key risks | Competing with mature POS ecosystems |
| Current status | **Likely Integrate**; folio posting may suffice early |

---

## Decision Guardrails

Before any item moves from candidate to approved MVP scope:

1. Target customer segment selected or pilot profile defined ([Target Customer](TARGET_CUSTOMER.md)) — **discovery segment is now selected; this guardrail alone does not approve MVP items**
2. Problem validated at minimum **E2** evidence where feasible ([Evidence Model](EVIDENCE_MODEL.md))
3. Build vs Integrate evaluated ([Build vs Integrate](BUILD_VS_INTEGRATE.md))
4. Explicit Product Owner approval recorded outside this document
5. No architecture or technology decisions assumed (Phase 2)

---

## Related Documents

- [Opportunity Matrix](OPPORTUNITY_MATRIX.md)
- [Build vs Integrate](BUILD_VS_INTEGRATE.md)
- [Target Customer](TARGET_CUSTOMER.md)
- [Roadmap](../roadmap/ROADMAP.md)
