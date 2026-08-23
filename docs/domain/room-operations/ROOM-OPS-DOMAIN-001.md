# ROOM-OPS-DOMAIN-001: Room Operations Foundation

## Status

**Accepted**

Product Owner + CTO approved baseline (2026-08-22). This is a **domain** decision record, not an architecture ADR. It freezes the Room Operations domain foundation and the intended Sprint 0.9B first slice in [FIRST_SLICE.md](FIRST_SLICE.md). Sprint 0.9A remains documentation only. This acceptance does **not** start Sprint 0.9B implementation.

Approved product/domain direction is **not** a validated universal hotel truth. The model is based on Product Owner expert workflow (E2 proxy for this discovery context) plus HuGuWeb reference/hospitality model (E0–E1). It may evolve when additional hotels, operational users, or pilots provide stronger evidence.

---

## Context

HuGuWeb is a hospitality-first ERP / PMS. Organization & Workforce Foundation is **Accepted** ([HR-DOMAIN-001](../hr/HR-DOMAIN-001-Organization-Workforce-Foundation.md)). The next operational question is not “build a Housekeeping CRUD module.”

It is: **who owns Oda, oda hazırlık, Kat Hizmetleri work, Ön Büro visibility, Minibar, Teknik Servis effects, and sellability?**

Constraints this model must not contradict:

- Modular monolith; modules added only when approved ([ADR-001](../../architecture/adr/ADR-001-Architecture-Style.md))
- Permission-based authorization; no department/position-name checks ([ADR-008](../../architecture/adr/ADR-008-Authorization-Strategy.md))
- Employee ≠ User ≠ Role ≠ Permission ≠ Position ([HR-DOMAIN-001](../hr/HR-DOMAIN-001-Organization-Workforce-Foundation.md))
- Property explicit; no tenant / hotel-group implementation
- No event sourcing, brokers, or outbox
- UI language is a user preference; technical identifiers stay English
- Employee mobile is future scope; web-first
- Strong MVP lean for housekeeping was **room-readiness coordination**, not a full HK platform ([MVP Candidates](../../product/MVP_CANDIDATES.md))
- “Don’t show modules. Show work.” ([Product Principles](../../product/PRODUCT_PRINCIPLES.md))

---

## Boundary

**In (conceptual domain):** minimal Room identity (first host), Room Readiness (Dirty/Clean/Inspected/Ready), HousekeepingWorkItem, RoomInspection, priority of that work, derived Sellability rules (documented), contracts for Stay checkout, FO block, TS serviceability, Minibar check.

**Out:** Reservations, Stay implementation, Folio, Minibar inventory/charging, Technical Service issues, Lost & Found, DND/No Service implementation, passcards, attendance/shift, generic task engine, notifications infrastructure, mobile app, SGK/KBS/e-Invoice, chat.

The slice is named **Oda Operasyonları / Room Operations**, not “Kat Hizmetleri Modülü.” Do **not** name the module Housekeeping.

Kat Hizmetleri is a **primary participant**, not the owner of all room operational state.

Future modules/domains may own:

- Stay / Reservations
- Front Office-specific block/sellability decisions
- Technical Service `MaintenanceIssue`
- Minibar
- generic operational tasks
- Lost & Found

---

## Decision

1. **Kat Hizmetleri does not own all room operational state.** Room Operations owns readiness. Kat Hizmetleri is the primary **participant**. Ön Büro, Minibar, and Teknik Servis participate through other facts. Reject Option A (HK-owned god status). Reject implementing Option C as two modules in 0.9B. Reject Option D (FO owns cleanliness).
2. **Host minimal `Room` identity here** (`Organization` → `Property` → `Room`). Future Reservations / Stay consume `RoomId`; they do not own physical identity. Do not create `RoomType` in Sprint 0.9B unless implementation later proves it unavoidable.
3. **Do not use one giant RoomStatus enum.** Dimensions: Readiness (owned now); Occupancy, Serviceability (OOO/OOS), Operational Block, Guest service restriction (separate); Sellability **derived**. Room Readiness is the only dimension entering Sprint 0.9B.
4. **Dirty → Clean → Inspected → Ready is one readiness machine**, not occupancy and not sellability. **Inspected is a real domain readiness state.** It must not be removed because a future Front Office UI may show only Dirty / Clean / Ready. Rejection requires a reason, not a photo, and returns work to Dirty / the attendant.
5. **Ready ≠ Sellable.** Blocked, OOO/OOS, occupancy, and hotel-specific gates (e.g. minibar wait) can prevent sale of a Ready room. Do not store Sellable as a master status in Sprint 0.9B.
6. **State ≠ task.** First implementation needs current readiness **and** a housekeeping work item (assignment, priority, rework). Not a generic workflow engine.
7. **Checkout is an incoming fact** (`StayCheckedOut` contract). No broker. Sprint 0.9B must **not** implement Reservations/Stay. Until Stay exists, a manual vacated/needs-cleaning stand-in is allowed. Do not name it a Checkout API if Checkout does not exist.
8. **Minibar is an independent operational dependency**, not a readiness value. Even if some hotels wait for Minibar, it remains a parallel operational gate. Do not implement minibar-pending in 0.9B.
9. **OOO, OOS, and Blocked are not one enum** and are not readiness values. TS owns issues later; FO owns Block later; Room Operations consumes effects. Do not persist Blocked, OOO, or OOS in Sprint 0.9B.
10. **Lost & Found, DND/No Service, passcards, discrepancy reporting** are documented boundaries only. Rahatsız Etmeyin and Hizmet İstemiyor are separate future guest-service restrictions, not RoomReadiness, and are out of 0.9B.
11. **Workforce:** reference `EmployeeId`. Do not duplicate Employee, Department, Position, Employment, or Assignment. Do not map Position to permission. Daily Employee → Floor roster is a valid future Kat Hizmetleri capability and is out of 0.9B.
12. **Coordination** is recorded actions (and later notifications from those actions), not chat.
13. **First production slice** is [FIRST_SLICE.md](FIRST_SLICE.md) — a separate Sprint 0.9B, not started here.

---

## Closed reference decisions

Product Owner + CTO closed the previous validation questions as follows. Evidence remains a discovery-hotel baseline, not universal hotel truth.

| Topic | Accepted direction |
|-------|--------------------|
| Stayover cleaning | The model must be capable of supporting stayover later. Sprint 0.9B remains checkout/vacated-centric. Do not implement Occupancy or Stay merely for stayover. |
| Yesterday’s Ready rooms | Ready does **not** automatically expire every morning. Ready remains until a real domain event invalidates it. A later daily revalidation procedure may create a separate inspection/revalidation work item. Do not add automatic Ready reset. |
| Minibar gate | Out of 0.9B. Parallel operational gate, not a RoomReadiness state. Do not implement minibar-pending. |
| Technical Service repair | Repair does not universally require Supervisor inspection. If the intervention affects room preparation/readiness, a new preparation/inspection cycle may be required. Otherwise repair alone should not automatically dirty the room. Technical Service remains out of 0.9B. |
| DND / No Service | Separate future guest-service restrictions. Not RoomReadiness. Out of 0.9B. |
| Floor roster | Valid future Kat Hizmetleri capability. Out of 0.9B. `HousekeepingWorkItem` → `EmployeeId` is sufficient for the first slice. |
| Blocked | Front Office-owned operational state. Do not persist in 0.9B. Keep Ready ≠ Sellable documented. |
| Inspected | A **real** domain readiness state. Do not remove it because a future FO UI may show only Dirty / Clean / Ready. |

---

## Accepted concepts

| Technical | Turkish product | Owner | Notes |
|-----------|-----------------|-------|--------|
| `Room` | Oda | Room Operations (first host) | Identity; not Stay |
| `RoomReadiness` | Oda hazırlık | Room Operations | Current Dirty/Clean/Inspected/Ready |
| `HousekeepingWorkItem` | Temizlik / hazırlık işi | Room Operations | Assignment, priority, rework |
| `RoomInspection` | Oda denetimi | Room Operations | Accept/reject + reason + history |
| `TaskPriority` | İş önceliği | On the work item | Domain data; auditable; first-level Normal / High / Urgent |
| `RoomBlock` | Bloke | Ön Büro later | Independent dimension; not persisted in 0.9B |
| `RoomServiceability` | Teknik elverişlilik | TS later; consumed here | OOO vs OOS vs available |
| `RoomServiceRestriction` | Hizmet kısıtı | Stay/FO later | DND / No Service |
| `Sellability` | Satışa uygunluk | Derived | Not a stored master status |
| `OperationalTask` | Genel operasyon işi | Future | Extra HK / guest request — **not** 0.9B |

Do not design EF navigations, repositories, or API endpoints in this record.

---

## Key decisions

| Topic | Choice |
|-------|--------|
| Boundary | Option B — Room Operations owns readiness; Kat Hizmetleri is primary participant |
| Module name (when built) | Room Operations, not Housekeeping |
| Room identity | Minimal Room now; RoomType later unless 0.9B proves it unavoidable |
| Status model | Multiple dimensions; readiness is one machine |
| Ready vs Sellable | Different; Sellable is not stored in 0.9B |
| OOO / OOS / Blocked | Three concepts, not readiness; none implemented in 0.9B |
| Minibar | Independent future dependency; not a readiness state |
| Lost & Found | Separate future |
| DND / No Service | Restrictions, not RoomReadiness |
| Inspected | Real readiness state |
| 0.9B | Room + readiness + assignment + inspection + view; not started by this record |

---

## Consequences

### Positive

- Matches real checkout → clean → inspect → ready without making HK the hotel.
- Leaves FO, TS, Minibar, and Stay able to exist without rewriting a HK god-module.
- Aligns with Operations Center “rooms need attention” UX without freezing Home buckets as the domain enum.
- Reuses Workforce without coupling auth to Position.

### Negative

- “Housekeeping module” expectation in the market must be explained as a participant.
- Sellability will look incomplete in 0.9B (intentionally).
- Two concepts (state + work) cost more than a single status field.

---

## Risks

| Risk | Mitigation |
|------|------------|
| 0.9B grows into full HK + TS + Minibar | Explicit OUT list in FIRST_SLICE |
| Ready treated as sellable in UI copy | Product language: Hazır vs Satışa uygun |
| Inspected dropped to please a three-bucket FO board | Domain fidelity over simplified UI buckets |
| Position-based auth sneaks in | ADR-008; permissions only |
| Empty Rooms + HK dual modules | One module until catalog split is justified |
| Treating PO workflow as universal | Evidence labels; closed questions remain a baseline, not a survey |

---

## Revisit conditions

- A Rooms/Inventory catalog product is approved and should take Room identity.
- Evidence that the target property skips physical inspection (would change 0.9B UX; it does **not** remove Inspected from the long-term model).
- A first-slice hotel requires automatic daily Ready revalidation as an operating dependency.
- A later Stay/Minibar/Technical Service slice must consume these contracts.

---

## Date

2026-08-22 (Sprint 0.9A)

---

## Related documents

- [README](README.md)
- [DOMAIN_BOUNDARY.md](DOMAIN_BOUNDARY.md)
- [ROOM_MODEL.md](ROOM_MODEL.md)
- [READINESS_MODEL.md](READINESS_MODEL.md)
- [HOUSEKEEPING_OPERATIONS.md](HOUSEKEEPING_OPERATIONS.md)
- [CROSS_DEPARTMENT_COORDINATION.md](CROSS_DEPARTMENT_COORDINATION.md)
- [INVARIANTS.md](INVARIANTS.md)
- [FIRST_SLICE.md](FIRST_SLICE.md)
