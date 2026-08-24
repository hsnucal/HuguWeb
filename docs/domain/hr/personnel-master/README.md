# Personel Master Data

> **Status:** Accepted — Product Owner + CTO approved reference baseline (Sprint HR-01A).
>
> Approved product/domain direction. **Not** a validated universal hotel truth. This folder does **not** authorize production code, EF entities, migrations, APIs, frontend screens, or government integrations.
>
> **HR-DOMAIN-001 (Organization & Workforce Foundation) remains Accepted and is not superseded.** This slice **extends** it. It does not replace Employee, Employment, Assignment, Department, Position, Organization, or Property.

Personel Master is the stable **identity / profile layer** for HuGuWeb Human Resources.

It answers: *who is this person, how do we identify them, how do we contact them, and what may later HR modules attach to?*

It does **not** answer leave, shifts, puantaj, payroll calculation, SGK/KBS submissions, documents, recruitment, or the employee portal.

**Evidence:** Accepted Workforce foundation (E0–E1, implemented through Sprint 0.8) plus **REFERENCE PRODUCT BEHAVIOR** from a Product-Owner-provided WebİK frontend snapshot. Reference behavior is not HuGuWeb domain truth. No hotel-user interviews (E2+) yet.

---

## Product language vs domain identity

| Audience | Language | Examples |
|----------|----------|----------|
| Product / hotel conversation | Turkish by default | Personel Kartı, Sicil No, TCKN, İban, Acil durum kişisi, İşe giriş |
| User-facing UI labels | Localized `tr` / `en` / `ru` | user preference |
| C# types, tables, enums, permission ids, API identifiers | Stable English | `Employee`, `NationalIdentityNumber`, `hr.employee.sensitive.read` |

Do **not** localize stored code identifiers (`Tckn`, `Active`, permission ids). Customer-defined Department and Position names remain **data**.

---

## One-sentence answer

**Personel Master extends the existing Employee with owned identity, contact, photo, and (later) payment-profile data. The Personel Card is a product composition surface over Employee, Employment, Assignment, and later HR modules — not one giant Employee aggregate.**

---

## Documents

| Document | Purpose |
|----------|---------|
| [HR-DOMAIN-002-Personnel-Master.md](HR-DOMAIN-002-Personnel-Master.md) | Domain decision record (**Accepted**) |
| [FIELD_CATALOG.md](FIELD_CATALOG.md) | Every candidate field: owner, HR-01 decision, sensitivity, list/import/export |
| [DATA_OWNERSHIP.md](DATA_OWNERSHIP.md) | Employee vs Employment vs Assignment vs Profile vs later modules |
| [PERSONNEL_CARD.md](PERSONNEL_CARD.md) | Card UX, tabs, unsaved-changes invariant, create/edit modes |
| [PERSONNEL_LIST.md](PERSONNEL_LIST.md) | List, filters, column picker |
| [PRIVACY_AND_PERMISSIONS.md](PRIVACY_AND_PERMISSIONS.md) | Classification, permissions, personas direction, operational DTO |
| [IMPORT_EXPORT.md](IMPORT_EXPORT.md) | Excel import/export, bulk photo (conceptual) |
| [INVARIANTS.md](INVARIANTS.md) | Binding rules for a later implementation |
| [FIRST_SLICE.md](FIRST_SLICE.md) | Frozen **HR-01B** / **HR-01C** split |

Related (do not contradict):

- [HR-DOMAIN-001](../HR-DOMAIN-001-Organization-Workforce-Foundation.md) — **Accepted**
- [HR-DOMAIN-003 Official Employment Data](../official-employment/README.md) — **Accepted** (HR-03A); does not change this folder’s Accepted status
- [WORKFORCE_MODEL.md](../WORKFORCE_MODEL.md) — Employee → Employment → Assignment
- [INVARIANTS.md](../INVARIANTS.md) — Accepted workforce invariants still hold
- [ADR-007](../../../architecture/adr/ADR-007-Authentication-Strategy.md) — Employee ≠ ApplicationUser
- [ADR-008](../../../architecture/adr/ADR-008-Authorization-Strategy.md) — permissions, never Position/Department names
- [LOCALIZATION.md](../../../product/LOCALIZATION.md) — UI language is a user preference

---

## REFERENCE PRODUCT OBSERVATIONS

Observed in the Product Owner–provided WebİK frontend snapshot. **Not** HuGuWeb domain truth.

- Large Employee / Personel Card as a modal overlay
- Create and edit use the same card
- Tabbed information architecture (Genel, Adres/Kimlik, official, pay deductions, Belgeler, Evraklar, Geçmiş, optional Performans/DISC)
- Configurable list columns with default-hidden sensitive columns
- Advanced search/filter
- Excel import (column mapping, TCKN match) and Excel export
- Bulk photo by filename (TC or sicil)
- Toplu Zam exists on the Personel list — belongs later in HuGuWeb
- Official fields (SGK, İŞKUR, KBS identity notification) depend on Personel Card data
- Unsaved-change guard on card close (✕ / Close / Escape / beforeunload)
- TCKN treated as required with checksum; sicil auto-assigned if blank
- Kademe is a rank ladder next to görev; Çalışma Grubu is Normal/Emekli/Engelli/Yabancı/Stajyer (payroll treatment), not a shift
- Emergency contacts as two flat name/phone pairs
- “Personeli Sil” exists (HuGuWeb rejects this as a normal action)
- Header mixed identity with SGK notified chips and portal access (HuGuWeb header will not)

---

## What HR-01A is

Documentation freeze only. Implementation belongs to a later **HR-01B** and is **not** started by this folder.

Do **not** modify Technical Service or Room Operations in this sprint.
