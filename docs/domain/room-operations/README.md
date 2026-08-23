# Oda Operasyonları / Room Operations

> **Status:** Accepted — Product Owner + CTO approved baseline (Sprint 0.9A).
>
> Approved product/domain direction. **Not** a validated universal hotel truth. Sprint 0.9A remains the accepted documentation baseline. Sprint 0.9B implementation is recorded in [SPRINT_0_9B_IMPLEMENTATION_NOTES.md](SPRINT_0_9B_IMPLEMENTATION_NOTES.md).

This folder defines the first **hotel-operations** domain slice: **Oda Operasyonları (Room Operations)**.

It is **not** a Kat Hizmetleri (Housekeeping) product, **not** a PMS Rooms module, **not** Reservations, and **not** Teknik Servis.

The product question is: *Hangi odalar dikkat gerektiriyor, ve oda satışa uygun mu?*  
The domain question is: *Oda kimliği, hazırlık, temizlik işi, denetim, teknik uygunluk ve operasyonel blok kimin gerçeğidir?*

**Evidence:** Product Owner supplied expert hotel workflow (treat as **E2 proxy** for this discovery hotel — not universal hotel truth) plus **E0–E1** HuGuWeb reference/hospitality model, now accepted as HuGuWeb’s Room Operations baseline. No hotel-user interviews and no pilots yet. See [Evidence Model](../../product/EVIDENCE_MODEL.md).

---

## Product language vs domain identity

| Audience | Language | Examples |
|----------|----------|----------|
| Product / hotel conversation | Turkish by default | Oda, Kat Hizmetleri, Kat Görevlisi, Supervisor, Order Taker, Ön Büro, Minibar, Teknik Servis, Kirli, Temiz, Denetimli, Hazır, Satışa uygun, Bloke, Arızalı (OOO), Hizmet dışı (OOS), Rahatsız Etmeyin, Hizmet İstemiyor, Kayıp Eşya |
| User-facing UI labels (later) | Localized `tr` / `en` / `ru` | user preference |
| C# types, tables, enums, permission ids, API identifiers | Stable English | `Room`, `RoomReadiness`, `HousekeepingWorkItem`, `RoomInspection` |

Do **not** translate technical identifiers per UI language. Customer-defined names (Employee, Department, Position, later Room commercial names) remain **data**.

---

## Accepted answer (one sentence)

**Oda Operasyonları owns Oda identity (first host) and Oda hazırlık / readiness. Kat Hizmetleri is the primary work participant, not the owner of all room operational state.**

Do **not** name the module Housekeeping.

---

## Documents

| Document | Purpose |
|----------|---------|
| [DOMAIN_BOUNDARY.md](DOMAIN_BOUNDARY.md) | Boundary options A–D; **Option B accepted** |
| [ROOM_MODEL.md](ROOM_MODEL.md) | Room identity and operational dimensions |
| [READINESS_MODEL.md](READINESS_MODEL.md) | Kirli → Temiz → Denetimli → Hazır; inspection/rejection |
| [HOUSEKEEPING_OPERATIONS.md](HOUSEKEEPING_OPERATIONS.md) | Cleaning work, priority, rework, Workforce |
| [CROSS_DEPARTMENT_COORDINATION.md](CROSS_DEPARTMENT_COORDINATION.md) | Ön Büro, Minibar, Teknik Servis, Rezervasyon, Kayıp Eşya, DND |
| [INVARIANTS.md](INVARIANTS.md) | Confirmed / accepted and reference-model rules |
| [FIRST_SLICE.md](FIRST_SLICE.md) | Frozen Sprint 0.9B scope |
| [SPRINT_0_9B_IMPLEMENTATION_NOTES.md](SPRINT_0_9B_IMPLEMENTATION_NOTES.md) | 0.9B implementation note |
| [ROOM-OPS-DOMAIN-001.md](ROOM-OPS-DOMAIN-001.md) | Domain decision record (**Accepted**) |

Related (do not contradict):

- [ADR-001](../../architecture/adr/ADR-001-Architecture-Style.md) — modular monolith; modules only when approved
- [ADR-008](../../architecture/adr/ADR-008-Authorization-Strategy.md) — permissions, never Position/Department names
- [HR-DOMAIN-001](../hr/HR-DOMAIN-001-Organization-Workforce-Foundation.md) — Workforce **Accepted**; reuse Employee
- [Teknik Servis / Maintenance](../maintenance/README.md) — Sprint 0.11A **Accepted** domain (0.11B not started)
- [MVP Candidates](../../product/MVP_CANDIDATES.md) — Housekeeping Strong lean is **readiness coordination**, not a full HK platform
- [Operations Center](../../design/OPERATIONS_CENTER.md) — Home shows work; statuses there are layout examples, not frozen domain enums
- [LOCALIZATION.md](../../product/LOCALIZATION.md) — UI language is a user preference; this sprint does not design i18n implementation

---

## What Sprint 0.9A is

Documentation only. Implementation belongs to Sprint 0.9B and is described in [SPRINT_0_9B_IMPLEMENTATION_NOTES.md](SPRINT_0_9B_IMPLEMENTATION_NOTES.md).
