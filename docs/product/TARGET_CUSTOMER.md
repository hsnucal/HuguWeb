# Target Customer Discovery

> **Status:** Approved initial discovery / pilot segment (Product Owner + CTO). Underlying segment assumptions remain **E0–E1** hypotheses — not customer-validated market truth.

**Approved direction ≠ validated market truth.** This document records where discovery and pilot preparation will focus. It does not claim the segment’s operational characteristics are proven.

---

## Approved Initial Discovery Segment

HuGuWeb will initially center product discovery and pilot preparation on:

> **Independent mid-size hotels with meaningful cross-department operational complexity.**

This is the **primary discovery segment**. It is not the only future market.

Do **not** define this segment by a fixed room-count range at this stage.

### Preferred behavioral / operational description

> A single-property independent hotel with multiple operational departments, meaningful cross-department coordination needs, and workflows that may currently require information to move between PMS software, spreadsheets, messaging tools, paper processes, or other disconnected systems.

Examples of spreadsheets, messaging tools, paper, and disconnected systems are **hypotheses to validate with real hotels**. They are not confirmed universal behavior.

**Evidence classification:** Approved Product Direction based primarily on **E0–E1** evidence. E2+ validation must come from real hotel-user/customer evidence. See [Evidence Model](EVIDENCE_MODEL.md).

Selecting this segment does **not** approve Inventory, Purchasing, Finance, Accounting, Maintenance, POS, HR, Employee Mobile, Booking Engine, Channel Manager, or multi-property UI as MVP. Those require workflow discovery and additional evidence ([MVP Candidates](MVP_CANDIDATES.md)).

---

## Why This Segment Was Selected

Reasoning below is concise and remains subject to validation.

### Small / Boutique Hotels

Not excluded from future sales.

They are not the initial product-design center because (hypotheses, not universal claims):

- operational complexity may be lower
- fewer people may manage several responsibilities
- full ERP value may be harder to demonstrate
- simpler tools may already be sufficient for some properties

### Independent Mid-Size Hotels

Selected as the primary discovery segment because they may provide a useful balance between:

- meaningful operational complexity
- multiple departments
- cross-department workflows
- ERP/PMS coordination needs
- manageable pilot implementation complexity
- lower initial enterprise integration burden than large resorts or chains

This remains subject to validation.

### Large Hotels / Resorts

Strategically relevant future segment.

Not selected for the earliest pilot because an unvalidated product would face:

- significantly higher workflow complexity
- integration expectations
- operational risk
- implementation risk
- reliability expectations
- reputational risk if the early product fails in critical operations

### Hotel Groups / Chains

Strategic long-term target.

Do not include chain-level complexity in MVP merely because chains are a future target.

Multi-property requirements should be considered early enough during future architecture decisions to avoid unnecessary redesign, but should not drive premature implementation. No architecture decision is made here.

---

## Growth Direction (Strategic Hypothesis)

Independent Mid-Size
→ validated pilots
→ broader independent / large hotel adoption
→ large hotels / resorts
→ multi-property capability
→ hotel groups / chains

This is a **strategic hypothesis**, not a guaranteed commercial roadmap. Real customer evidence may change the sequence.

---

## Purpose

HuGuWeb is a hospitality-first ERP / PMS platform for hotels. This document compares potential customer segments so discovery stays honest about trade-offs.

Room counts, staff sizes, and similar figures below are **illustrative only**. They are not a segment definition and are not a factual threshold.

---

## Segment Comparison Overview

| Dimension | Independent Small / Boutique | Independent Mid-Size | Large Resort / Large Independent | Hotel Group / Chain |
|-----------|------------------------------|----------------------|----------------------------------|---------------------|
| Pain complexity | Lower–Medium | Medium–High | High | High |
| Purchasing complexity | Lower | Medium–High | High | High |
| Implementation complexity | Lower | Medium | High | Very High |
| Revenue potential (per property) | Lower | Medium | Higher | Higher (portfolio) |
| Integration needs | Lower–Medium | Medium | High | Very High |
| Fit for first pilot | Not initial design center | **Selected discovery / pilot focus** | Future strategic; not earliest pilot | Long-term strategic; not earliest pilot |
| Differentiation opportunity | Medium (hypothesis) | High (hypothesis) | Medium–High (hypothesis) | Medium (hypothesis; requires scale) |

Qualitative ratings are **E0 hypotheses** (some **E1** market context for typical scale complexity). They are not rankings of proven market truth.

**How not to read this table**

- Independent Mid-Size is the **approved discovery/pilot focus**, not a claim that it is the only viable market or that its hypothesized pains are proven.
- A “High” differentiation rating is a research hypothesis, not a conclusion.
- **Fit for first pilot** (feedback speed, implementation risk, controllable scope) is a different question from **long-term commercial attractiveness** (contract value, expansion, strategic fit). Do not collapse those into one ranking.
- Room-count language elsewhere in this document is illustrative only—not a factual threshold or segment definition.

---

## Independent Small / Boutique Hotel

**Status:** Not excluded from future sales. Not the initial product-design center.

### Possible characteristics (hypothesis)

- Lower room count (illustrative range often cited in industry: roughly tens of rooms — **not** a cutoff)
- Smaller teams with staff wearing multiple hats
- Limited or no dedicated IT resources
- Simpler operational processes
- High sensitivity to ease of use, implementation speed, and price
- May rely heavily on spreadsheets, paper, or a single legacy PMS (hypothesis to validate)

### Qualitative evaluation

| Dimension | Assessment | Notes |
|-----------|------------|-------|
| Pain complexity | Lower–Medium | Operational pain exists but may involve fewer departments and handoffs |
| Purchasing complexity | Lower | Fewer approval layers; simpler inventory needs |
| Implementation complexity | Lower | Shorter onboarding; fewer integrations and departments |
| Revenue potential | Lower | Per-property contract value likely smaller |
| Integration needs | Lower–Medium | May still need OTA, payments, and basic finance connectivity |
| Fit for first pilot | Not initial design center | Complexity and ERP-value demonstration may be lower (hypothesis) |
| Differentiation opportunity | Medium (hypothesis) | Simplicity and connected workflows *may* stand out vs fragmented tools |

### Research hypotheses to validate

- Is fragmented software still a major pain at this scale, or is a basic PMS sufficient?
- Would back-office capabilities (purchasing, inventory, HR) matter early, or feel like overkill?
- Is price sensitivity high enough to block adoption of a broader platform?

---

## Independent Mid-Size Hotel

**Status:** Primary discovery / pilot segment. Approved direction; operational assumptions unvalidated.

### Possible characteristics (hypothesis)

- Moderate room count (illustrative: roughly dozens to low hundreds of rooms — **not** a cutoff or definition)
- More distinct departments (front office, housekeeping, F&B, maintenance, finance)
- Stronger process complexity across operational boundaries
- Greater need for purchasing, inventory, housekeeping coordination, reporting, and HR-related workflows
- May already use multiple disconnected systems (hypothesis to validate)

### Qualitative evaluation

| Dimension | Assessment | Notes |
|-----------|------------|-------|
| Pain complexity | Medium–High | Cross-department handoffs become more frequent and costly |
| Purchasing complexity | Medium–High | More suppliers, stock items, and approval paths |
| Implementation complexity | Medium | More departments to onboard; more integration touchpoints |
| Revenue potential | Medium | Better per-property economics than very small properties |
| Integration needs | Medium | PMS, POS, payments, possibly local compliance integrations |
| Fit for first pilot | **Selected discovery / pilot focus** | Hypothesized balance of complexity vs implementability; requires validation |
| Differentiation opportunity | **High (hypothesis)** | Current research signal only: connected hotel workflows *may* resonate; requires validation |

### Research hypotheses to validate

- Which cross-department workflows cause the most daily friction?
- Is operational visibility (reporting, approvals, status) a purchasing driver?
- Would this segment accept phased rollout (core PMS first, back-office next)?
- Are inventory and purchasing first-pilot necessities here, later differentiators, or scope traps if included too early?
- Do information flows actually move between PMS, spreadsheets, messaging tools, paper, or other disconnected systems—and which of those matter?

---

## Large Resort / Large Independent Hotel

**Status:** Strategically relevant future segment. Not selected for the earliest pilot.

### Possible characteristics (hypothesis)

- High room count and diverse facilities (illustrative: hundreds of rooms or more — **not** a cutoff)
- Complex F&B operations, events, spa, or recreation areas
- Significant staff count across many departments
- Maintenance, staff transportation, and staff accommodation may be relevant
- Procurement complexity and multi-vendor relationships
- Stronger integration requirements with POS, channel managers, revenue tools, and enterprise finance

### Qualitative evaluation

| Dimension | Assessment | Notes |
|-----------|------------|-------|
| Pain complexity | High | Many parallel workflows and dependencies |
| Purchasing complexity | High | Central and departmental purchasing; larger inventory scope |
| Implementation complexity | High | Longer rollout; change management risk |
| Revenue potential | Higher | Larger per-property value if successfully adopted |
| Integration needs | High | Mature ecosystem expectations; less tolerance for gaps |
| Fit for first pilot | Future strategic; not earliest pilot | Higher workflow, integration, operational, and reputational risk before product maturity |
| Differentiation opportunity | Medium–High (hypothesis) | Differentiation *possible* if core workflows are solved well; risk of feature expectations |

### Research hypotheses to validate

- Would a new platform be considered before core PMS parity expectations are met?
- Which integrations are non-negotiable on day one?
- Is maintenance, F&B, or HR coordination a stronger entry point than front office?

---

## Hotel Group / Chain

**Status:** Strategic long-term target. Do not include chain-level complexity in MVP merely because chains are a future target.

### Possible characteristics (hypothesis)

- Multi-property portfolio under shared ownership or management
- Central procurement and consolidated finance/reporting needs
- Centralized HR policies with property-level operations
- Property-level permissions and role models
- Cross-property analytics and benchmarking interest
- Stronger vendor evaluation and security/compliance scrutiny

### Qualitative evaluation

| Dimension | Assessment | Notes |
|-----------|------------|-------|
| Pain complexity | High | Pain exists at both property and group level |
| Purchasing complexity | High | Central vs local purchasing models vary |
| Implementation complexity | Very High | Multi-property rollout, governance, and data isolation |
| Revenue potential | Higher | Portfolio deals; expansion within group |
| Integration needs | Very High | ERP, BI, identity, compliance, and partner ecosystems |
| Fit for first pilot | Long-term strategic; not earliest pilot | Architecture and scope demands likely exceed early MVP |
| Differentiation opportunity | Medium (hypothesis) | Opportunity *may* exist but likely requires multi-property readiness not yet defined |

### Research hypotheses to validate

- Is group-level consolidation or property-level agility the stronger buying trigger?
- How do Tenant, Property, and Hotel Group concepts need to relate (see [Glossary](GLOSSARY.md))?
- Would a group accept piloting on a single property first?

---

## Cross-Segment Themes

Current internal hypotheses (not validated):

1. **Fragmentation pain** may increase with operational scale—from small properties using too many tools, to mid-size properties suffering handoff failures, to large properties and groups needing integration coherence.
2. **Pilot feasibility** currently centers on independent mid-size properties. Small/boutique hotels remain possible later customers; large resorts and chains remain later strategic scope.
3. **Differentiation** *may* be stronger where daily cross-department workflows (front office ↔ housekeeping ↔ finance visibility) are frequent—often hypothesized for mid-size and larger independent properties. This is **E0–E1**, not validated market truth.
4. **Multi-property** requirements are strategically relevant long-term but should not drive premature MVP implementation (see [Future Scope](FUTURE_SCOPE.md)). Future architecture decisions should consider them early enough to avoid unnecessary redesign. No architecture decision is made here.

---

## Open Decisions

Decided: initial discovery / pilot focus is Independent Mid-Size (this document).

Still open:

- Which operational assumptions about this segment hold in real hotels (**E2+** required)
- Whether evidence will change the growth-direction sequence
- Minimum property profile for pilot eligibility (departments, existing systems, operational complexity)—no room-count cutoff is defined
- Inventory and Purchasing: first-pilot necessity vs later differentiator vs scope trap (**not decided here**; selecting the segment does not promote these to MVP)
- Commercial model implications by segment (not defined in this document)
- Whether Turkey/local market properties differ materially from global segment patterns

---

## Related Documents

- [Hotel Problems](HOTEL_PROBLEMS.md)
- [Discovery Questions](DISCOVERY_QUESTIONS.md)
- [MVP Candidates](MVP_CANDIDATES.md)
- [Evidence Model](EVIDENCE_MODEL.md)
- [Market Research](../research/MARKET_RESEARCH.md)
- [Future Scope](FUTURE_SCOPE.md)
