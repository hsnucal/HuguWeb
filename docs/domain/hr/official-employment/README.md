# Official Employment Data

> **Status:** Accepted — Product Owner + CTO approved reference baseline (Sprint HR-03A), with **Accepted Product Owner amendment — 2026-08-24** before HR-03B commit.
>
> HR-03B implementation is authorized **under that amendment**. SGK/KBS/İŞKUR **clients**, credentials, notification tables, workers, and outbox/broker infrastructure remain out.
>
> **HR-DOMAIN-001 remains Accepted and is not superseded.**
> **HR-DOMAIN-002 remains Accepted and is not superseded.**
>
> This slice **extends** Organization & Workforce Foundation and Personel Master. It does not replace Employee, Employment, Assignment, Department, Position, Organization, Property, or EmployeeHrProfile.

Official Employment Data is the stable **statutory classification layer** for HuGuWeb Human Resources.

It answers: *how is this employment classified for Turkish official reporting, and which of this property’s SGK workplace registrations applies to that employment?*

It does **not** answer SGK submission, KBS identity notification, İŞKUR monthly **report filing**, payroll calculation, documents, or the employee portal. Resmî bilgiler may **compose** İŞKUR/BES/social/education **master facts** (PO amendment) without owning those government/payroll products.

**Evidence:** Accepted Workforce foundation and Personel Master (HR-01A/01B) plus **REFERENCE PRODUCT BEHAVIOR** from the Product-Owner-provided WebİK frontend snapshot (`WebİK — İnsan Kaynakları.html` / `ik.webik.com.tr.zip`). Reference behavior is not HuGuWeb domain truth and does not prove legal/server behavior.

---

## Product language vs domain identity

| Audience | Language | Examples |
|----------|----------|----------|
| Product / hotel conversation | Turkish by default | Resmî bilgiler, Bildirge Kodları, Belge türü, Tabi kanun, Sigorta kolu, Meslek kodu, İşyeri sicil, SGK İşyeri |
| User-facing UI labels | Localized `tr` / `en` / `ru` | user preference |
| C# types, tables, enums, permission ids, API identifiers | Stable English | `OfficialEmploymentProfile`, `SgkWorkplaceRegistration`, `SgkOccupationCode` |

Do **not** localize stored official codes (`01`, `05510`, `00`, `5120.10`). Display names are translated only when HuGuWeb owns the label; official Turkish descriptions stay as reference data.

---

## One-sentence answer

**A Property may have many SGK workplace registrations. Employment owns a current `OfficialEmploymentProfile` that may reference the applicable registration and stores employee-specific statutory codes. Resmî bilgiler is a composition tab. Saving it does not submit anything to SGK, KBS, or İŞKUR and does not calculate payroll.**

---

## Documents

| Document | Purpose |
|----------|---------|
| [HR-DOMAIN-003-Official-Employment-Data.md](HR-DOMAIN-003-Official-Employment-Data.md) | Domain decision record (**Accepted**) |
| [FIELD_CATALOG.md](FIELD_CATALOG.md) | Every HR-03A candidate field: owner, requiredness class, sensitivity |
| [PROPERTY_OFFICIAL_CONFIGURATION.md](PROPERTY_OFFICIAL_CONFIGURATION.md) | Property-level SGK workplace registrations (0..*) |
| [EMPLOYMENT_OFFICIAL_PROFILE.md](EMPLOYMENT_OFFICIAL_PROFILE.md) | Employment-level statutory classification |
| [LOOKUP_CODES.md](LOOKUP_CODES.md) | Lookup families and source-extracted option lists |
| [PERSONNEL_CARD_OFFICIAL_TAB.md](PERSONNEL_CARD_OFFICIAL_TAB.md) | Resmî bilgiler tab IA; Bildirge Kodları section |
| [PERMISSIONS_AND_PRIVACY.md](PERMISSIONS_AND_PRIVACY.md) | Classification, permissions, operational minimization |
| [INVARIANTS.md](INVARIANTS.md) | Binding rules for a later implementation |
| [FIRST_SLICE.md](FIRST_SLICE.md) | Frozen **HR-03B** / deferred **HR-03C+** split |

Related (do not contradict):

- [HR-DOMAIN-001](../HR-DOMAIN-001-Organization-Workforce-Foundation.md) — **Accepted**
- [HR-DOMAIN-002](../personnel-master/HR-DOMAIN-002-Personnel-Master.md) — **Accepted**
- [WORKFORCE_MODEL.md](../WORKFORCE_MODEL.md) — Employee → Employment → Assignment
- [ORGANIZATION_MODEL.md](../ORGANIZATION_MODEL.md) — Organization ≠ Property; Property stays thin; Employment does not own Property
- [personnel-master/PERSONNEL_CARD.md](../personnel-master/PERSONNEL_CARD.md) — Accepted card IA; this slice unhides Resmî bilgiler
- [personnel-master/PRIVACY_AND_PERMISSIONS.md](../personnel-master/PRIVACY_AND_PERMISSIONS.md) — Accepted permission split
- [LOCALIZATION.md](../../../product/LOCALIZATION.md) — UI language is a user preference

---

What HR-03A **was:** documentation freeze. HR-03B implementation proceeds under the 2026-08-24 Product Owner amendment.

Do **not** modify Technical Service or Room Operations in this sprint.
