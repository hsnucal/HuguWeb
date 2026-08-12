# Product Evidence Model

> **Status:** Discovery framework — not a gate for every minor decision.

This model defines how HuGuWeb product claims should mature from internal assumptions to pilot-backed evidence.

**What this model is:** a check against `internal idea → requirement` without validation.

**What this model is not:** a bureaucracy where every minor UX copy change, layout tweak, or engineering hygiene decision needs a customer interview.

See also [Product Principles](PRODUCT_PRINCIPLES.md) — "Evidence before scope."

---

## Evidence Levels

### E0 — Assumption

**Definition:** Internal idea, hypothesis, or intuition only. No external validation.

**Examples:**

- "Mid-size independent hotels may suffer most from cross-department handoff failures"
- "Housekeeping mobile app is required for MVP"

**Usage:** Acceptable for brainstorming and discovery planning. **Not** sufficient for MVP scope approval.

---

### E1 — Market Signal

**Definition:** Competitor, market, or desk research evidence exists—but not yet confirmed with target hotel users.

**Examples:**

- Hospitality software commonly spans fragmented PMS, POS, and finance tools ([Market Research](../research/MARKET_RESEARCH.md))
- Channel managers are widely used in hospitality distribution (general market context)
- Competitor listed as research input ([Competitor Analysis](../research/COMPETITOR_ANALYSIS.md))

**Usage:** Supports research direction and opportunity hypotheses. **Not** sufficient alone for major build commitments.

---

### E2 — User Problem Evidence

**Definition:** At least one relevant hotel user (or credible proxy) confirms the problem exists and matters in their context.

**Examples:**

- Front office manager describes learning room readiness via phone calls
- Purchasing lead confirms approval delays cause operational stockouts

**Usage:** Strong input for prioritizing discovery and interview follow-ups. Single-user evidence may be segment-specific.

---

### E3 — Repeated Validation

**Definition:** Multiple relevant users or organizations confirm the same problem or priority pattern.

**Examples:**

- Three independent properties describe similar folio-to-accounting manual exports
- Housekeeping supervisors across interviews cite late priority room information

**Usage:** Suitable for Product Owner + CTO prioritization discussions and MVP **candidates**—still not automatic approval.

---

### E4 — Pilot Evidence

**Definition:** Behavior or data from actual HuGuWeb pilot use validates outcome, adoption, or workflow improvement.

**Examples:**

- Pilot property reduces check-in delays measurable against baseline
- Housekeeping status updates adopted by majority of floor staff without workaround

**Usage:** Strongest basis for scope expansion, classification changes, and post-MVP investment.

---

## How Evidence Affects Priority

| Evidence progression | Typical product action |
|---------------------|------------------------|
| E0 → E1 | Continue research; refine hypotheses |
| E1 → E2 | Prioritize interview depth; map to [Hotel Problems](HOTEL_PROBLEMS.md) |
| E2 → E3 | Elevate in [Opportunity Matrix](OPPORTUNITY_MATRIX.md) discussion; consider conditional MVP candidacy |
| E3 → E4 | Support MVP scope freeze elements and Phase 4 validation metrics |

Features and capabilities should gain **stronger priority** as evidence increases.

---

## Practical Rules

### Do not require interviews for every small decision

The evidence model applies to **product claims that create scope**: new capabilities, MVP inclusion, build-vs-integrate bets, regulatory ownership, and segment selection.

It does **not** apply to:

- Minor UX details (labels, layout, empty states)
- Internal documentation wording
- Engineering hygiene, tests, and refactoring
- Implementation choices inside an already-approved capability

Do not invent a process where every ticket needs E2.

### Do require stronger evidence for high-impact scope

The following should not proceed on E0 alone:

- Full accounting build vs integrate
- Multi-property architecture commitments
- Regulatory integration ownership
- Large modules (POS, revenue management, CRM) as internal build
- MVP scope freeze ([Roadmap](../roadmap/ROADMAP.md) Phase 1)

### Label documents honestly

When writing product docs, prefer:

- "Current hypothesis (E0)"
- "Market signal (E1)"
- "Reported in interview (E2)" — when recorded
- "Validated across N properties (E3)" — when recorded
- "Pilot observed (E4)" — when available

Avoid stating "validated" without specifying level.

---

## Approved Direction vs Validated Market Truth

An **approved product direction** may be recorded so discovery and pilots have a focus.

That is not the same as **validated market truth**.

The Independent Mid-Size targeting decision in [Target Customer](TARGET_CUSTOMER.md) is an **Approved Product Direction based primarily on E0–E1 evidence**.

- The decision itself is approved for discovery focus.
- The underlying assumptions about the segment are **not** customer-validated.
- E2+ validation must come from real hotel-user/customer evidence.

> Approved direction ≠ validated market truth.

Do not treat the targeting decision as E2, E3, or E4 evidence.

---

## Current Sprint 0.2 Evidence Baseline

**No E2, E3, or E4 evidence exists yet.** No hotel-user interviews and no HuGuWeb pilots have occurred.

Most Sprint 0.2 outputs are **E0–E1**:

| Document | Dominant evidence level |
|----------|-------------------------|
| [Target Customer](TARGET_CUSTOMER.md) | Approved discovery direction on **E0–E1**; segment assumptions unvalidated |
| [Hotel Problems](HOTEL_PROBLEMS.md) | E0–E1 (problem hypotheses + market context) |
| [Opportunity Matrix](OPPORTUNITY_MATRIX.md) | E0–E1 (qualitative candidates) |
| [MVP Candidates](MVP_CANDIDATES.md) | E0–E1 (candidates, not approved) |
| [Build vs Integrate](BUILD_VS_INTEGRATE.md) | E0–E1 (principle + market patterns) |
| [Competitor Analysis](../research/COMPETITOR_ANALYSIS.md) | E1 where general market context; many entries pending sources |

Interview execution should aim to produce **E2/E3** for priority problems.

---

## Related Documents

- [Target Customer](TARGET_CUSTOMER.md)
- [Discovery Questions](DISCOVERY_QUESTIONS.md)
- [Product Principles](PRODUCT_PRINCIPLES.md)
- [MVP Candidates](MVP_CANDIDATES.md)
- [Development Workflow](../engineering/DEVELOPMENT_WORKFLOW.md)
