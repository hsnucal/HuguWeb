# Organization & Workforce Foundation

> **Status:** Accepted — Product Owner + CTO approved baseline (Sprint 0.7A).
>
> Approved product/domain direction. **Not** a validated universal hotel truth. The model may evolve when hotel HR experts, operational users, pilot hotels, or official integration specifications provide stronger evidence.

This folder defines **Organization & Workforce Foundation** — the first HR-related domain. It is **not** a complete İnsan Kaynakları (Human Resources) product and **not** payroll.

It provides the foundational workforce model later hotel operations will need: who works here, in which department, in which position, with history that survives transfer and termination.

**Evidence:** E0–E1 reference model, now accepted as HuGuWeb’s implementation baseline ([Evidence Model](../../product/EVIDENCE_MODEL.md)). No hotel-user interviews (E2+) yet. Do not treat minor later corrections as process failure.

---

## Product language vs domain identity

During product discussions, use **Turkish** terminology where practical. Internal technical identifiers stay **English** and locale-stable.

| Audience | Language | Examples |
|----------|----------|----------|
| Product / hotel conversation | Turkish by default | Personel, Departman, Pozisyon, Görevlendirme, İş ilişkisi, Sicil No, Tesis, Organizasyon / Şirket, Kat Hizmetleri, Ön Büro |
| User-facing UI labels | Localized `tr` / `en` / `ru` | user preference ([LOCALIZATION.md](../../product/LOCALIZATION.md)) |
| C# types, tables, enums, permission ids, API contract identifiers | Stable English | `Employee`, `Department`, `EmploymentStatus.Active` |

Do **not** translate type names, table names, enum values, permission identifiers, or API contract identifiers.

Customer-defined Department and Position names are **data**, not HuGuWeb i18n keys. Sprint 0.7B stores one working name per record. UI language remains user-specific.

See [DOMAIN_MODEL.md](DOMAIN_MODEL.md#localization-strategy).

---

## Documents

| Document | Purpose |
|----------|---------|
| [DOMAIN_MODEL.md](DOMAIN_MODEL.md) | Boundary, concepts, aggregates, localization, official-notification readiness |
| [ORGANIZATION_MODEL.md](ORGANIZATION_MODEL.md) | Organization, Property, Department, Position |
| [WORKFORCE_MODEL.md](WORKFORCE_MODEL.md) | Employee, Employment, Assignment |
| [INVARIANTS.md](INVARIANTS.md) | Consistency rules for implementation |
| [FIRST_SLICE.md](FIRST_SLICE.md) | Frozen Sprint 0.7B production slice |
| [SPRINT_0_7B_IMPLEMENTATION_NOTES.md](SPRINT_0_7B_IMPLEMENTATION_NOTES.md) | Sprint 0.7B module/auth/persistence notes |
| [SPRINT_0_8_WORKFORCE_UX_VALIDATION.md](SPRINT_0_8_WORKFORCE_UX_VALIDATION.md) | Sprint 0.8 local launcher + Workforce UX validation |
| [HR-DOMAIN-001](HR-DOMAIN-001-Organization-Workforce-Foundation.md) | Domain decision record (**Accepted**) |

Related architecture (do not contradict):

- [ADR-001](../../architecture/adr/ADR-001-Architecture-Style.md) — modular monolith; no premature modules in code
- [ADR-007](../../architecture/adr/ADR-007-Authentication-Strategy.md) — Identity stays at Host
- [ADR-008](../../architecture/adr/ADR-008-Authorization-Strategy.md) — permissions, not department/position names
- [TECHNOLOGY_DECISIONS.md](../../architecture/TECHNOLOGY_DECISIONS.md) — Property explicit; Tenant / Hotel Group not implemented; no brokers at bootstrap
- [LOCALIZATION.md](../../product/LOCALIZATION.md) — UI language is a user preference
- [MVP_CANDIDATES.md](../../product/MVP_CANDIDATES.md) — Full HR / Payroll is **not** MVP

---

## What Sprint 0.7A is

Documentation only. No production C# domain code, EF entities, migrations, APIs, frontend changes, government integrations, or placeholder integration services.
