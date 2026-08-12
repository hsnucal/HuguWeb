# Market Research

> **Status:** Consolidated research context — not a product scope document.

This document holds market research context for HuGuWeb. Sprint 0.2 added ERP vs PMS landscape distinction and hospitality platform opportunity hypothesis. **Market validation has not occurred.**

---

## Target Market

HuGuWeb's first target industry is **hotels and hospitality**.

HuGuWeb is being designed as a hospitality-first ERP / PMS platform—not a generic ERP for every industry. Future expansion into other industries may be considered later, but early product decisions must reflect real hotel operational needs.

Target customer segments are documented in [Target Customer](../product/TARGET_CUSTOMER.md). **Initial discovery / pilot focus:** independent mid-size hotels with meaningful cross-department operational complexity. This is an approved discovery direction on E0–E1 evidence, not validated market truth, and not the only future market.

---

## ERP-Centric Products

### Typical strengths (market context — E1)

ERP-centric products often emphasize:

- Finance and accounting depth
- Purchasing and supplier management
- Inventory and warehouse control
- HR, payroll, and enterprise controls
- Statutory reporting and compliance (market-dependent)
- Cross-department process enforcement and auditability

### Typical limitations in hospitality context (hypothesis — E0/E1)

ERP-first products may be weaker at:

- Daily guest-stay operational workflows (check-in/out, room status)
- Housekeeping and front office real-time coordination
- Reservation and room inventory as operational hub
- Hospitality distribution integrations (OTA, channel manager)

These are **hypotheses** requiring validation through competitor study and customer interviews—not universal claims about specific vendors.

### HuGuWeb relevance

ERP benchmarks inform Build vs Integrate boundaries for finance, inventory, purchasing, and HR ([Build vs Integrate](../product/BUILD_VS_INTEGRATE.md)). HuGuWeb is **not** aiming to replicate full ERP breadth early.

---

## PMS-Centric Products

### Typical strengths (market context — E1)

PMS-centric products often emphasize:

- Reservations and availability management
- Room and guest stay operations
- Front office workflows
- Housekeeping coordination
- Folio and in-stay charge handling
- OTA and channel manager integrations (often partner-based)

### Typical limitations in hospitality context (hypothesis — E0/E1)

PMS-first products may be weaker at:

- Deep back-office purchasing and inventory control
- Full accounting and enterprise financial close
- Broad HR and payroll depth
- Cross-property ERP-style consolidation

These are **hypotheses** for discovery—not claims about specific products without evidence.

### HuGuWeb relevance

PMS benchmarks define core operational workflow expectations. HuGuWeb's differentiation hypothesis is **not** "more PMS features alone" but potentially **better-connected operations** across departments.

---

## Hospitality Platform Opportunity

### Current HuGuWeb hypothesis (E0 — not market-validated)

A potential market opportunity may exist in connecting:

- **Guest-facing operations** (reservation, front office, rooms, housekeeping, folio)
- **Back-office workflows** (purchasing, inventory, finance hooks, approvals)
- **Staff workflows** (maintenance, HR operational requests—future mobile scope)

…more coherently than using disconnected PMS + ERP + spreadsheets.

This aligns with the **Hotel Operating System** hypothesis in [Product Vision](../product/PRODUCT_VISION.md):

> Hotel workflows may communicate across operational boundaries (e.g., checkout affecting room status, housekeeping, folio, and invoicing).

**This remains a hypothesis.** Do not claim market validation has occurred.

### Fragmentation context (repository documented)

Hotels typically rely on a combination of:

- Property Management Systems (PMS)
- Channel managers and OTA integrations
- Point of Sale (POS) systems
- Financial and accounting software
- Housekeeping and maintenance tools
- HR and payroll systems
- Revenue management systems

This fragmentation **may** create operational friction. HuGuWeb's product vision includes reducing fragmented software usage where practical, while integrating with mature external systems where building is not strategic.

---

## Strategic Positioning Hypothesis

HuGuWeb may differentiate by:

- Solving real hotel operational problems rather than maximizing feature count
- Workflow-oriented UX ("Don't show modules. Show work.")
- Connected hotel workflows across operational boundaries
- Integration-friendly architecture with clear Build vs Integrate decisions

These require validation through [Discovery Questions](../product/DISCOVERY_QUESTIONS.md), [Evidence Model](../product/EVIDENCE_MODEL.md) progression, and eventual pilot feedback.

---

## Research Areas Under Investigation

The following domains are being researched. They are **research areas**, not approved MVP modules:

| Domain | Notes |
|--------|-------|
| PMS / Reservations | Strong MVP candidate group — not approved |
| Front Office | Strong MVP candidate group — not approved |
| Guests / identity | Minimal identity implied by stay workflows; distinct Guest Profiles is Conditional / Needs More Research |
| Rooms | Strong MVP candidate group (room inventory/status) — not approved |
| Housekeeping | Strong for room-readiness coordination; full HK platform unvalidated |
| Inventory | Conditional; **not** assumed first-pilot; differentiator vs scope trap — open decision |
| Purchasing | Conditional; **not** assumed first-pilot; differentiator vs scope trap — open decision |
| Finance visibility / hooks | Scope boundary unclear; not the same as folio or full accounting |
| Accounting (statutory) | Likely integrate-first hypothesis — HuGuWeb must not automatically become an accounting product |
| Human Resources | Future / integrate-first hypothesis |
| Maintenance | Next / conditional candidate |
| Reporting | Conditional candidate |
| Integrations | Channel, POS, payments, compliance |

See [Opportunity Matrix](../product/OPPORTUNITY_MATRIX.md) and [MVP Candidates](../product/MVP_CANDIDATES.md).

---

## Multi-Property and Hotel Chains

Hotel chains and multi-property management are strategically relevant to HuGuWeb's long-term direction.

Future architecture decisions should evaluate multi-property requirements early enough to avoid expensive redesign. No multi-property functionality is designed or implemented during the foundation stage.

Concepts requiring formal definition before implementation: **Tenant**, **Hotel / Property**, **Hotel Group**. These are not automatically equivalent ([Glossary](../product/GLOSSARY.md)).

---

## Open Research Questions

The following require Product Owner and CTO attention:

- What are the highest-pain operational problems for target hotel segments? → [Hotel Problems](../product/HOTEL_PROBLEMS.md)
- Which capabilities are best built vs integrated? → [Build vs Integrate](../product/BUILD_VS_INTEGRATE.md)
- What is the minimum viable scope for pilot hotel validation? → [MVP Candidates](../product/MVP_CANDIDATES.md)
- How do hotel chains vs independent properties differ in requirements? → [Target Customer](../product/TARGET_CUSTOMER.md)
- Are Inventory and Purchasing first-pilot necessities, later differentiators, or scope traps?
- Which finance layers belong in HuGuWeb vs an accounting system (events, folio, payments, visibility, integration, statutory books)?
- What regulatory and compliance requirements apply in target markets (e.g., e-Invoice, government reporting)?
- Which competitor patterns matter for pilot segment vs long-term chain segment?

---

## Related Documents

- [Competitor Analysis](COMPETITOR_ANALYSIS.md)
- [Target Customer](../product/TARGET_CUSTOMER.md)
- [Hotel Problems](../product/HOTEL_PROBLEMS.md)
- [Opportunity Matrix](../product/OPPORTUNITY_MATRIX.md)
- [Product Vision](../product/PRODUCT_VISION.md)
- [Product Principles](../product/PRODUCT_PRINCIPLES.md)
- [Future Scope](../product/FUTURE_SCOPE.md)
- [Roadmap](../roadmap/ROADMAP.md)
