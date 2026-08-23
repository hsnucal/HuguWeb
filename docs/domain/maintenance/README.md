# Teknik Servis / Technical Service

> **Status:** Accepted — Product Owner + CTO approved reference baseline (Sprint 0.11A).
>
> Approved product/domain direction. **Not** a validated universal hotel truth. Sprint 0.11A remains documentation only. This acceptance does **not** start Sprint 0.11B.

This folder defines the **Teknik Servis (Technical Service)** domain: hotel arıza coordination, not a CMMS, not asset/facility management, and not a generic task platform.

The product question is: *Oda teknik olarak kullanılabilir mi, ve arıza kimin işi?*  
The domain question is: *Arıza, öncelik, atama, çözüm, teknik elverişlilik ve OOO/OOS kimin gerçeğidir?*

**Evidence:** Product Owner / expert supplied hotel workflow (**EXPERT-SUPPLIED WORKFLOW**, treated as E2 proxy for this discovery hotel) plus **ACCEPTED PRODUCT CONTEXT** from Room Operations and Workforce, plus **REFERENCE MODEL**, now accepted as HuGuWeb’s Technical Service baseline. No hotel-user interviews and no pilots yet. See [Evidence Model](../../product/EVIDENCE_MODEL.md).

This sprint does **not** authorize production code, EF entities, migrations, APIs, or screens.

---

## Product language vs domain identity

| Audience | Language | Examples |
|----------|----------|----------|
| Product / hotel conversation | Turkish by default | Teknik Servis, Arıza, İş Emri, Oda, Ön Büro, Kat Hizmetleri, Aynı gün arıza (OOO), Hizmet dışı (OOS), Bloke, Teknik elverişlilik |
| User-facing UI labels (later) | Localized `tr` / `en` / `ru` | user preference |
| C# types, tables, enums, permission ids, API identifiers | Stable English | `MaintenanceIssue`, `OutOfOrder`, `OutOfService`, `Serviceable` |

Do **not** translate technical identifiers per UI language.

**İş Emri** remains valid hotel language. The first-slice **aggregate** is `MaintenanceIssue` (Arıza), not a separate `MaintenanceWorkOrder`. See [ISSUE_MODEL.md](ISSUE_MODEL.md).

---

## Accepted answer (one sentence)

**Teknik Servis owns Arıza (`MaintenanceIssue`) and the facts that make an oda technically unusable. Oda Operasyonları owns hazırlık. Ön Büro later owns Bloke and oda değişikliği. Sellability is derived from all three — never stored as a master status by this domain.**

Do **not** name the module Housekeeping. Do **not** reuse `HousekeepingWorkItem`.

---

## Documents

| Document | Purpose |
|----------|---------|
| [DOMAIN_BOUNDARY.md](DOMAIN_BOUNDARY.md) | What Teknik Servis owns; what it must not absorb |
| [ISSUE_MODEL.md](ISSUE_MODEL.md) | Arıza vs İş Emri, lifecycle, category, priority, source, resolution |
| [ROOM_SERVICEABILITY.md](ROOM_SERVICEABILITY.md) | Teknik elverişlilik, OOO/OOS, blocking, Ready vs Serviceable vs Sellable |
| [WORKFLOW.md](WORKFLOW.md) | Report → assign → intervene → resolve / unable |
| [CROSS_DEPARTMENT_COORDINATION.md](CROSS_DEPARTMENT_COORDINATION.md) | Ön Büro, Kat Hizmetleri / Oda Operasyonları, future Stay |
| [INVARIANTS.md](INVARIANTS.md) | Binding rules for a later implementation |
| [FIRST_SLICE.md](FIRST_SLICE.md) | Frozen Sprint **0.11B** scope (not started) |
| [MAINT-DOMAIN-001.md](MAINT-DOMAIN-001.md) | Domain decision record (**Accepted**) |

Related (do not contradict):

- [ROOM-OPS-DOMAIN-001](../room-operations/ROOM-OPS-DOMAIN-001.md) — **Accepted.** Ready ≠ Sellable; OOO/OOS are not RoomReadiness; Room identity hosted there
- [HR-DOMAIN-001](../hr/HR-DOMAIN-001-Organization-Workforce-Foundation.md) — **Accepted.** Reuse Employee; Employee ≠ User ≠ Permission ≠ Position
- [ADR-001](../../architecture/adr/ADR-001-Architecture-Style.md) — modular monolith; modules only when approved
- [ADR-008](../../architecture/adr/ADR-008-Authorization-Strategy.md) — permissions, never Position/Department names
- [MVP Candidates](../../product/MVP_CANDIDATES.md) — Maintenance is a conditional **Next** candidate, not an IFS-like CMMS
- [Operations Center](../../design/OPERATIONS_CENTER.md) — Home shows work; wireframe statuses are layout examples
- [LOCALIZATION.md](../../product/LOCALIZATION.md) — UI language is a user preference

---

## What Sprint 0.11A is

Documentation and domain design only. Implementation belongs to a later Sprint **0.11B** and is **not** started by this folder.
