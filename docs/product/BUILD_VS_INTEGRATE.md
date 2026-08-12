# Build vs Integrate Research

> **Status:** High-level candidate recommendations — no vendors selected.

---

## Principle

HuGuWeb should **build** capabilities that create strategic product differentiation and **integrate** mature external capabilities when internal development creates excessive cost, risk, or maintenance burden.

This principle appears in [Product Principles](PRODUCT_PRINCIPLES.md) and [Product Vision](PRODUCT_VISION.md). It does not automatically reject building any capability—it requires explicit evaluation.

---

## Recommendation Values

| Value | Meaning |
|-------|---------|
| **Build Candidate** | Strategic to own internally; differentiation or workflow coherence benefits likely outweigh cost |
| **Integrate Candidate** | Mature external ecosystem; internal build likely poor ROI or high risk |
| **Hybrid Candidate** | Core workflow owned internally; specialized functions delegated to integrations |
| **Needs More Research** | Insufficient evidence on complexity, market expectations, or pilot requirements |

---

## Evaluation Summary

| Capability | Strategic Importance | Complexity | Maintenance Burden | Integration Availability | Current Recommendation |
|------------|---------------------|------------|-------------------|-------------------------|------------------------|
| Channel Manager | High | High | High | High (mature market) | **Integrate Candidate** |
| OTA connectivity | High | High | High | High (via channel/PMS ecosystems) | **Integrate Candidate** |
| Payment Provider | High | Medium–High | High (PCI/compliance) | High | **Integrate Candidate** (folio may be build; capture integrates) |
| POS | High (F&B properties); lower if room-only | Very High | Very High | High | **Integrate Candidate** — urgency is **segment-dependent** |
| e-Invoice / e-Archive | High (market-dependent) | High | High | Medium–High (local providers) | **Needs More Research** (Integrate likely if legally required) |
| Government reporting / identity | High (market-dependent) | High | High | Medium (jurisdiction-specific) | **Needs More Research** (Integrate likely if mandated) |
| Payroll | Medium–High (HR ops); may not block a PMS pilot | High | High | High | **Integrate Candidate** — whether *any* payroll data exchange is needed for pilot: **Needs More Research** |
| Revenue Management | Medium–High; lower for early operational validation | Very High | Very High | High | **Integrate Candidate** |
| Booking Engine | Medium; **segment-dependent** | Medium–High | Medium–High | High | **Needs More Research** (Integrate or Hybrid remain options) |
| Full statutory accounting | High for the hotel’s books; **not** automatic HuGuWeb product | Very High | Very High | High (local ERP) | **Integrate Candidate** or **Needs More Research** — do not become an accounting company |
| Finance visibility / accounting integration | Medium–High | High | High | High | **Needs More Research** (distinct from folio and from full accounting) |

---

## Channel Manager

### Strategic importance

High for properties relying on OTA and online distribution. Likely a purchasing expectation for many segments, but not a strong HuGuWeb differentiation hypothesis.

### Complexity

High — connectivity to many channels, rate/availability sync, error handling, and partner API churn.

### Maintenance burden

High — external API changes, mapping rules, and support load.

### Integration availability

High — established hospitality channel platforms and PMS partner ecosystems.

### Current recommendation

**Integrate Candidate**

HuGuWeb may own reservation and inventory truth internally while delegating channel distribution to partners. Hybrid patterns (core PMS + channel integration layer) may apply.

---

## OTA Connectivity

### Strategic importance

High where OTAs drive bookings. Often accessed indirectly through channel managers rather than direct bilateral integrations.

### Complexity

High — multiple OTAs, content rules, and reconciliation.

### Maintenance burden

High — continuous partner change management.

### Integration availability

High — typically via channel manager or consolidated connectivity providers.

### Current recommendation

**Integrate Candidate**

Direct OTA build is unlikely to be strategic unless a very specific market gap is validated with evidence.

---

## Payment Provider

### Strategic importance

High — folio settlement and guest payments are operationally critical.

### Complexity

Medium–High — PCI scope, refunds, pre-auth, multi-currency, and reconciliation.

### Maintenance burden

Medium–High — security compliance and provider API updates.

### Integration availability

High — many payment gateways and hospitality payment integrations exist.

### Current recommendation

**Integrate Candidate**

HuGuWeb folio/charge workflow may be a **Build Candidate**; payment *capture* likely integrates. Which providers, which flows (pre-auth, deposits, refunds), and PCI boundary are **market- and segment-dependent**. No vendor selected.

---

## POS

### Strategic importance

High for F&B-heavy properties; lower for room-only boutique properties.

### Complexity

Very High — menus, modifiers, kitchen routing, hardware, fiscal requirements in some markets.

### Maintenance burden

Very High — device ecosystems and fiscal rule changes.

### Integration availability

High — mature POS vendors with hospitality integrations.

### Current recommendation

**Integrate Candidate**

**Hybrid Candidate** pattern: internal folio posting + external POS for F&B operations. Whether this matters for a first pilot depends on the property’s F&B profile (room-only boutique vs F&B-heavy). **Needs More Research** on urgency, not on whether HuGuWeb should build a POS.

---

## e-Invoice / e-Archive

### Strategic importance

High in markets where electronic invoicing is legally required (e.g., Turkey's e-Fatura/e-Arşiv context is a research area, not validated in this repo).

### Complexity

High — legal formats, serial management, cancellation flows, and authority integrations.

### Maintenance burden

High — regulatory changes.

### Integration availability

Medium–High — local ERP and compliance providers (Logo, Uyumsoft, etc. listed as ERP benchmarks).

### Current recommendation

**Needs More Research** — **Integrate Candidate** is the likely lean *if* the pilot market legally requires it.

Market-specific evidence required before any build commitment. Listing local ERP names as research benchmarks is **not** vendor selection.

---

## Government Reporting / Identity Integrations

### Strategic importance

High where guest identity reporting to authorities is mandated (requirements vary by country/region).

### Complexity

High — jurisdiction-specific formats, deadlines, and audit expectations.

### Maintenance burden

High — regulatory change risk.

### Integration availability

Medium — often via local PMS integrations or government-approved middleware; varies by market.

### Current recommendation

**Needs More Research** — **Integrate Candidate** is the likely lean *if* the pilot jurisdiction mandates it.

Must be validated for the pilot market with sourced compliance requirements. Do not treat “hotels report guests to authorities somewhere” as a build plan.

---

## Payroll

### Strategic importance

Medium–High for HR operations; may not block initial PMS pilot if handled externally.

### Complexity

High — tax rules, social contributions, payslip formats, and labor law changes.

### Maintenance burden

High — regulatory and calculation updates.

### Integration availability

High — payroll providers and local ERP HR modules.

### Current recommendation

**Integrate Candidate** for payroll *calculation*.

Whether a first PMS-centric pilot needs any payroll data exchange at all is **Needs More Research** and segment-dependent. HuGuWeb should not become a payroll product. Employee mobile remains [Future Scope](FUTURE_SCOPE.md).

---

## Revenue Management

### Strategic importance

Medium–High for revenue-focused properties; less critical for initial operational workflow validation.

### Complexity

Very High — forecasting, pricing algorithms, competitive set analysis.

### Maintenance burden

Very High — models, data feeds, and tuning.

### Integration availability

High — dedicated RMS vendors and PMS partnerships.

### Current recommendation

**Integrate Candidate**

Unlikely to be an early build unless HuGuWeb strategy pivots to commercial/pricing tech as the core product (not the current hypothesis). Importance is **segment-dependent** (revenue-focused properties vs simpler independents).

---

## Booking Engine

### Strategic importance

Medium — supports direct bookings and brand channel; may matter more for certain independent segments.

### Complexity

Medium–High — availability, payments, promotions, and web presence.

### Maintenance burden

Medium–High — security, UX, and marketing integration.

### Integration availability

High — standalone booking engines, website plugins, and PMS-native options.

### Current recommendation

**Needs More Research**

**Integrate Candidate** or **Hybrid Candidate** remain options after segment validation. Direct-booking importance varies (some independents care; some are OTA-heavy). Do not assume HuGuWeb will own a booking surface.

---

## Full Statutory Accounting

### Strategic importance

High for the hotel’s legal books. **Not** automatically HuGuWeb’s product. Folio, payment handling, finance visibility, accounting integration, and full statutory accounting are different layers ([MVP Candidates](MVP_CANDIDATES.md)).

### Complexity

Very High — GL, AP/AR, tax, statutory reports, local chart-of-accounts practice.

### Maintenance burden

Very High — regulatory change.

### Integration availability

High — local ERP and accounting products are the usual system of record for books.

### Current recommendation

**Integrate Candidate** or **Needs More Research**

Do **not** decide that HuGuWeb is an accounting company. Operational folio may still be a build candidate without owning the GL.

---

## Finance Visibility / Accounting Integration

### Strategic importance

Medium–High if managers need an operational money-picture during the day, or if finance re-keys PMS data.

### Complexity

High — mapping operational events to accounting objects without becoming a GL.

### Maintenance burden

High if HuGuWeb owns mappings and tax rules; lower if it only exports.

### Integration availability

High — export, posting APIs, and accountant-side tools exist; exact pattern is market-dependent.

### Current recommendation

**Needs More Research**

Distinct from folio (operational) and from full accounting (statutory). No vendor selected.

---

## Build Candidates (Core Platform Hypothesis)

The following are **not** in the integration list above but represent areas HuGuWeb *may* need to **build** for a PMS-centric stay workflow—subject to MVP approval:

| Area | Rationale (hypothesis) |
|------|------------------------|
| Reservation / availability core | Operational system of record |
| Front office workflows | Daily operational hub |
| Room management & status | Foundation for housekeeping connection |
| Housekeeping room-readiness | Operational dependency with front office — not a full HK platform |
| Minimal guest identity | Needed on reservation/stay/folio — **not** a Guest Profiles product |
| Folio / guest charges | Stay settlement — **not** statutory accounting |
| Operational reporting hooks | “Show work” visibility — depth unvalidated |

See [MVP Candidates](MVP_CANDIDATES.md) — all are candidates, not approved build scope. Guest Profiles, Inventory, and Purchasing are **not** listed as build candidates here.

---

## Open Decisions

- Pilot market regulatory requirements (e-Invoice, identity reporting)
- Minimum integration set acceptable for first pilot property
- Hybrid boundary definitions (what data HuGuWeb owns vs exchanges)
- Booking engine: integrate, hybrid, or defer — **segment-dependent**
- Finance layers: which of events / folio / payments / visibility / integration / full accounting belong in HuGuWeb vs partners
- Inventory and Purchasing: first-pilot necessity vs differentiator vs scope trap
- Integration partner selection (deferred — **no vendors in this document**)
- Whether any integrate-first area eventually becomes build for differentiation

---

## Related Documents

- [Opportunity Matrix](OPPORTUNITY_MATRIX.md)
- [MVP Candidates](MVP_CANDIDATES.md)
- [Competitor Analysis](../research/COMPETITOR_ANALYSIS.md)
- [Product Principles](PRODUCT_PRINCIPLES.md)
