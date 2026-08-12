# Opportunity Matrix

> **Status:** Qualitative candidate framework — not frozen product scope or MVP decisions.

This is a **decision aid** for Product Owner + CTO. It is not a second copy of [MVP Candidates](MVP_CANDIDATES.md) or [Build vs Integrate](BUILD_VS_INTEGRATE.md).

- Grouping (Strong / Conditional / later) → [MVP Candidates](MVP_CANDIDATES.md)
- Integrate vs build lean → [Build vs Integrate](BUILD_VS_INTEGRATE.md)
- Ratings here are **E0–E1** hypotheses. Do not treat Low/Medium/High as scores.

When uncertainty is high, prefer **Needs More Research**.

---

## Dimensions (abbreviated in the table)

| Abbr | Dimension | Question |
|------|-----------|----------|
| Val | Hotel Business Value | Could this improve hotel operations? |
| Freq | Usage Frequency | How often would staff rely on it? |
| Crit | Operational Criticality | How disruptive is its absence day-to-day? |
| Diff | Differentiation Potential | Could HuGuWeb stand out here? |
| Buy | Purchase Decision Impact | Does this influence buying? |
| Impl | Implementation Complexity | How hard to deliver well? |
| Maint | Maintenance Cost | Ongoing / regulatory burden |
| Integ | Integration Opportunity | Is a mature external alternative a better fit? |
| Reg | Regulatory / Compliance Risk | Legal, tax, or reporting exposure if built |
| Conf | Product Confidence | Confidence given current research |

## Classification

| Classification | Meaning |
|----------------|---------|
| **MVP Candidate** | Appears central to core stay operations if a PMS-centric entry is validated |
| **Next** | Valuable after core operations; not confirmed for first release |
| **Future** | Strategically relevant later |
| **Integrate Candidate** | Mature external ecosystems may beat a full internal build |
| **Needs More Research** | Insufficient evidence to classify confidently |
| **Reject Candidate** | Poor fit (none currently — do not reject without evidence) |

---

## Matrix

| Capability | Val | Freq | Crit | Diff | Buy | Impl | Maint | Integ | Reg | Conf | Classification candidate |
|------------|-----|------|------|------|-----|------|-------|-------|-----|------|--------------------------|
| Reservation / PMS Core | High | High | High | Med | High | High | High | Med | Low–Med | Med | **MVP Candidate** |
| Front Office | High | High | High | Med | High | Med–High | Med–High | Low–Med | Low | Med | **MVP Candidate** |
| Room Management | High | High | High | Med | High | Med | Med | Low | Low | Med | **MVP Candidate** |
| Housekeeping (room-readiness) | High | High | High | High* | Med–High | Med | Med | Low–Med | Low | Med | **MVP Candidate** (readiness coordination, not full HK platform) |
| Folio / Guest Charges | High | High | High | Med | High | Med–High | Med | Med | Med | Med | **MVP Candidate** (operational folio only) |
| Guest identity / Profiles | Med–High | High | Med | Med | Med | Med | Med | Med | Med (privacy) | Med | **Needs More Research** (identity vs profiles — see notes) |
| Inventory | Med–High | Med–High | Med | High* | Med | Med–High | Med–High | Med | Low | Low–Med | **Needs More Research** (conditional Next; not first-pilot assumed) |
| Purchasing | Med–High | Med | Med | High* | Med | Med–High | Med–High | Med | Low–Med | Low–Med | **Needs More Research** (conditional Next; not first-pilot assumed) |
| Finance visibility / hooks | High | Med | Med–High | Med–High | Med–High | High | High | High | High | Low | **Needs More Research** |
| Full Accounting | High | Med | Med ops / High close | Med | Med–High | Very High | Very High | High | Very High | Low | **Integrate Candidate** or **Future** |
| HR | Med | Med | Med | Med | Med | High | High | High | Med–High | Low | **Future** or **Integrate Candidate** |
| Employee Mobile App | Med | Med–High | Med | Med | Med | High | High | Low | Med | Low | **Future** |
| Maintenance | Med–High | Med | Med–High | Med–High | Med | Med–High | Med | Med | Low | Low–Med | **Next** or **Needs More Research** |
| POS / F&B | High† | High† | High† | Low–Med | Med–High | Very High | Very High | Very High | Med | Low | **Integrate Candidate** |
| Channel Manager | High | Med | High | Low | High | High | High | Very High | Low | Med | **Integrate Candidate** |
| Booking Engine | Med–High | Med | Med | Low–Med | Med | Med–High | Med–High | High | Low | Low | **Needs More Research** (Integrate or Hybrid — segment-dependent) |
| Revenue Management | High | Med | Med / High commercial | Low | Med | Very High | Very High | Very High | Low | Low | **Integrate Candidate** |
| CRM (advanced) | Med | Med | Low–Med | Med | Med | Med–High | Med–High | High | Med | Low | **Future** or **Integrate Candidate** |
| Reporting / Dashboard | High | Med–High | Med | High* | Med–High | Med–High | Med | Med | Low | Low–Med | **Needs More Research** (conditional Next) |
| Multi-property UI | High‡ | Med | Low early / High chains | Med | High‡ | High | High | Low | Low | Low | **Future** |
| Multi-property architectural awareness | High (long-term) | N/A | N/A | Med | Med‡ | High | High | Low | Low | Low | **Needs More Research** (no architecture decision) |
| e-Invoice / e-Archive | High§ | Med–High | High§ | Low | Med–High | High | High | Very High | Very High | Low | **Integrate Candidate** or **Needs More Research** |
| Government / identity reporting | High§ | Med | High§ | Low | Med–High | High | High | Very High | Very High | Low | **Integrate Candidate** or **Needs More Research** |

\* Differentiation “High” is an **E0 hypothesis**, not a conclusion.  
† Where F&B exists.  
‡ For groups/chains.  
§ Where legally required.

---

## Notes that change a decision

Only notes that affect classification or scope boundary. Do not restate the table.

**Stay-core (MVP candidates if PMS-centric entry is validated)**

- **Reservation / Front Office / Room Management:** Operational dependencies for selling and occupying rooms. Differentiation, if any, is connected workflow—not feature parity with other PMS products.
- **Housekeeping:** Strong only as **room-readiness coordination with front office**. Full housekeeping operations (assignment, inspection programs, linen) are unvalidated. Employee mobile remains [Future Scope](FUTURE_SCOPE.md).
- **Folio:** Operational guest charges and stay settlement. **Not** payment capture, finance visibility, accounting integration, or statutory books.

**Guest identity vs profiles**

- Some guest identity on reservation/stay/folio is implied by stay-core.
- A distinct **Guest Profiles** capability (history, search, preferences) is **not** justified as MVP Candidate on current evidence. Competitor PMS commonly have profiles; that is not sufficient.

**Inventory and Purchasing — explicit open decision**

Do **not** decide here. Tension to preserve:

- **Not** assumed required for first-pilot value.
- **May** differentiate a hospitality ERP vs PMS-only tools (especially mid-size / F&B-heavy — hypothesis).
- **May** be a **scope trap** if included early to resemble generic ERP.
- Importance **likely changes** by small vs mid-size vs resort. Independent Mid-Size is the discovery focus; that does **not** decide Inventory/Purchasing inclusion. Product Owner + CTO decision still required.

**Finance layers (not one product)**

These are different scopes. HuGuWeb must **not** automatically become an accounting company.

| Layer | Lean |
|-------|------|
| Operational financial events | Near folio / front office |
| Folio / guest charges | MVP candidate (operational) |
| Payment handling | Integrate candidate |
| Finance visibility | Needs More Research |
| Accounting integration | Integrate candidate |
| Full statutory accounting | Integrate or Future — not automatic HuGuWeb scope |

**Integrate-leaning (complexity / ecosystems, not “we will pick a vendor”)**

- **Channel Manager / OTA:** Strongest integrate case on current evidence (distribution complexity, partner churn). Still a candidate, not a signed partnership.
- **POS, Revenue Management, Payroll (under HR):** Mature specialized domains; internal build is a poor early bet. Urgency depends on segment (e.g. F&B-heavy vs room-only).
- **Booking Engine:** Segment- and direct-booking-strategy dependent. **Needs More Research** — Integrate or Hybrid remain options.
- **e-Invoice / government reporting:** Market-specific. Integrate or Needs More Research until pilot geography is known. No vendors selected.

**Explicitly later**

- **Employee Mobile App** and **Multi-property UI** remain future. Multi-property *architectural awareness* may stay strategically important without being implemented. No architecture work in this sprint.

---

## Strongest current opportunities (hypothesis)

1. Connected stay operations — reservation, rooms, front office, housekeeping readiness, folio
2. Cross-department visibility that reduces handoff failure — unvalidated
3. Back-office connection (inventory / purchasing / finance hooks) **only if** segment evidence supports it — otherwise a scope trap
4. Integration discipline for channel, POS, payments, compliance, payroll

## Major uncertainties / open decisions

- Initial discovery segment selected: Independent Mid-Size ([Target Customer](TARGET_CUSTOMER.md)). Segment assumptions still require **E2+** validation. Selecting the segment does **not** promote Inventory, Purchasing, or other conditional items to MVP.
- Whether evidence will change the growth-direction sequence (mid-size → broader independent / large hotels → resorts → multi-property → chains)
- Inventory / Purchasing: first-pilot necessity vs differentiator vs scope trap
- Finance layer boundary (events / folio / payments / visibility / integration / full accounting)
- F&B/POS integration requirements by segment
- Local regulatory integration burden
- Booking engine timing and ownership

---

## Related Documents

- [MVP Candidates](MVP_CANDIDATES.md)
- [Build vs Integrate](BUILD_VS_INTEGRATE.md)
- [Target Customer](TARGET_CUSTOMER.md)
- [Evidence Model](EVIDENCE_MODEL.md)
- [Hotel Problems](HOTEL_PROBLEMS.md)
