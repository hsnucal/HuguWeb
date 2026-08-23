# MAINT-DOMAIN-001: Technical Service / Maintenance Foundation

## Status

**Accepted**

Product Owner + CTO approved reference baseline (2026-08-23). This is a **domain** decision record, not an architecture ADR. It freezes the Teknik Servis domain foundation and the intended Sprint 0.11B first slice in [FIRST_SLICE.md](FIRST_SLICE.md). Sprint 0.11A remains documentation only. This acceptance does **not** start Sprint 0.11B implementation.

Approved product/domain direction is **not** a validated universal hotel truth. The model is based on Product Owner expert workflow (E2 proxy for this discovery context) plus Accepted Room Operations / Workforce context plus HuGuWeb reference/hospitality model. It may evolve when additional hotels, operational users, or pilots provide stronger evidence.

---

## Context

HuGuWeb is a hospitality-first ERP / PMS. Organization & Workforce Foundation is **Accepted**. Room Operations Foundation is **Accepted** ([ROOM-OPS-DOMAIN-001](../room-operations/ROOM-OPS-DOMAIN-001.md)), including:

- Ready ≠ Sellable
- OOO / OOS are **not** RoomReadiness
- Teknik Servis is separate from RoomReadiness
- Room Operations hosts minimal Room identity
- RoomReadiness = Dirty / Clean / Inspected / Ready only

The next operational question is not “build a CMMS.”

It is: **what is the smallest correct Teknik Servis domain so that Arıza, atama, çözüm, teknik elverişlilik, and Ön Büro / Kat Hizmetleri handoff are honest?**

Constraints this model must not contradict:

- Modular monolith; modules added only when approved ([ADR-001](../../architecture/adr/ADR-001-Architecture-Style.md))
- Permission-based authorization; no department/position-name checks ([ADR-008](../../architecture/adr/ADR-008-Authorization-Strategy.md))
- Employee ≠ User ≠ Role ≠ Permission ≠ Position ([HR-DOMAIN-001](../hr/HR-DOMAIN-001-Organization-Workforce-Foundation.md))
- Property explicit; no tenant / hotel-group implementation
- No event sourcing, brokers, or outbox
- UI language is a user preference; technical identifiers stay English
- Employee mobile is future scope; web-first
- Strong lean: hotel reactive coordination, not IFS-like maintenance ([MVP Candidates](../../product/MVP_CANDIDATES.md))
- “Don’t show modules. Show work.” ([Product Principles](../../product/PRODUCT_PRINCIPLES.md))
- Do not reuse `HousekeepingWorkItem` as generic technical work

---

## Boundary

**In (conceptual domain):** `MaintenanceIssue`, category reference data, priority, Employee assignment, blocking + OOO/OOS classification, derived room serviceability, resolution / unable-to-resolve, preparation-impact declaration, issue history, contracts for Room Operations consume and future Ön Büro.

**Out:** CMMS, assets, PM, parts, vendors, SLA, cost, photos, chat/notifications infrastructure, mobile, common-area location platform, oda değişikliği, Reservations/Stay, Bloke, RoomReadiness writes, generic task engine, `MaintenanceWorkOrder` as a second aggregate.

The slice is named **Teknik Servis / Technical Service**, not “Bakım Modülü” and not “İş Emri Platformu.”

Teknik Servis **owns:**

- `MaintenanceIssue` / Arıza
- technical issue lifecycle
- technical assignment
- blocking technical impact
- OOO/OOS classification
- technical serviceability facts
- repair result/history

Teknik Servis does **not** own:

- Room identity
- RoomReadiness
- Dirty / Clean / Inspected / Ready
- `HousekeepingWorkItem`
- Bloke
- Stay
- Reservation
- Room Change
- Sellability master state
- asset-management platform
- preventive maintenance
- parts / inventory

---

## Decision

1. **Teknik Servis owns Arıza and technical unavailability facts.** Oda Operasyonları owns hazırlık. Ön Büro later owns Bloke and oda değişikliği. Reject folding OOO into RoomReadiness. Reject pulling Bloke into TS because all three affect availability.
2. **First slice uses one aggregate: `MaintenanceIssue`.** Assignment and work lifecycle live on the issue. Reject a separate `MaintenanceWorkOrder` until a real second work execution appears. UI may still say İş Emri.
3. **Do not store OOO/OOS as a master status on `Room`.** They are outage classifications on a **blocking** issue. Current room serviceability is **derived** (serviceable vs unavailable + OOO or OOS label). This is the consumed view Room Operations already named; not a third readiness machine.
4. **Not every open Arıza blocks the oda.** `BlocksRoomUse` is explicit. Minor faults can remain open while the oda stays technically usable.
5. **OOO vs OOS** = operator expected-duration class (same day vs not), not a clock, not severity, not Bloke. Required only when blocking. If several blockers exist, the house label is the more restrictive (`OutOfService` over `OutOfOrder`).
6. **Lifecycle:** `Open` → `InProgress` → `Resolved`, or `Open` → `InProgress` → `UnableToResolve` → `InProgress` → `Resolved`. Assigned is data. `InProgress` requires `AssignedEmployeeId`. `Resolved` is terminal. `UnableToResolve` is not terminal and may resume to `InProgress`. Recurrence after `Resolved` is a new `MaintenanceIssue`. Do not add `Closed`.
7. **Category** is property-scoped reference data (flat). Do not hard-code trades.
8. **Priority only** in 0.11B (`Normal` / `High` / `Urgent`). Severity is deferred. No scoring engine.
9. **Reporter** is optional Employee + origin note. Not ApplicationUser-only. Not a department enum. Employee ↔ User linking remains unimplemented. History actor may be `ApplicationUser` for now.
10. **First slice is oda-only** (`RoomId` required). Description stands in for equipment. No `Location`, `Area`, `Asset`, or `Equipment` abstractions in 0.11B.
11. **Workforce:** `EmployeeId` assignment, Active employment. No Position matching. First slice may assign any Active Employee because safe technician eligibility does not yet exist. That limitation is documented, not faked.
12. **Permissions (later implementation):** `maintenance.read` / `maintenance.manage` / `maintenance.resolve`. No `maintenance.report` yet. No runtime roles such as Technician or MaintenanceManager. Authorization is permission claims only.
13. **Resolver declares `PreparationImpact`.** TS does not set Dirty/Clean/Inspected/Ready. `None` must not reset readiness. `RequiresPreparation` is a thin in-process consume for Room Operations.
14. **Coordination is recorded facts**, later notifications from those facts — not chat.
15. **First production slice** is [FIRST_SLICE.md](FIRST_SLICE.md) — a separate Sprint 0.11B, **not started here**.

---

## Closed reference decisions

Product Owner + CTO closed the previous validation questions as follows. Evidence remains a discovery-hotel baseline, not universal hotel truth.

| Topic | Accepted direction |
|-------|--------------------|
| Blocking authority | Ön Büro / a reporting user **may** initially report that the fault blocks room use. Teknik Servis may validate or change this during handling. The issue owns `BlocksRoomUse`. Do not implement a separate Front Office domain in 0.11B. Do not encode authority with department or Position strings. Authorization remains permission-based. |
| UnableToResolve vs availability | `UnableToResolve` does **not** restore technical serviceability if the issue is blocking. Guest movement / room change does **not** resolve the technical fault. Blocking continues until the issue is `Resolved` or the blocking flag is explicitly changed by a valid business action. No Front Office Room Change implementation in 0.11B. |
| OOO / OOS timing | Remain operator classification (same-day vs not). Do **not** derive automatically from a datetime or SLA clock. |
| Preparation-impact actor | The resolving Technical Service operator declares `PreparationImpact`. TS must not write Dirty / Clean / Inspected / Ready. Future Supervisor review/override may be added later if evidence requires it. |
| Common-area first slice | **0.11B is room-only.** `MaintenanceIssue` requires `RoomId`. Do not create Location / Area / Asset / Equipment abstractions in 0.11B. |
| Multiple blockers | Multiple blocking issues **may** exist for one Room. Derived house display: any active blocking OOS → `OutOfService`; else any active blocking OOO → `OutOfOrder`; else `Serviceable`. Do not enforce a single blocker per Room. |
| InProgress | Required start on the accepted paths. Assigned is data, not a state. `InProgress` requires `AssignedEmployeeId`. |
| Resume after UnableToResolve | Same Arıza may resume `UnableToResolve` → `InProgress`. Recurrence after `Resolved` is a **new** `MaintenanceIssue`. |
| RequiresPreparation consume | If the Room is already Dirty with an appropriate active housekeeping work item, **reuse** that preparation flow; do not create duplicate cleaning work. If no preparation cycle/work exists, Room Operations starts the appropriate preparation / needs-cleaning behavior. Implementation details belong to 0.11B. Thin in-process integration only; no broker/outbox. |
| Front Office report permission | Do **not** create `maintenance.report` in 0.11B. Smallest set: `maintenance.read`, `maintenance.manage`, `maintenance.resolve`. |

---

## Accepted concepts

| Technical | Turkish product | Owner | Notes |
|-----------|-----------------|-------|--------|
| `MaintenanceIssue` | Arıza | Teknik Servis | First-slice aggregate; may display as İş Emri |
| `MaintenanceIssueCategory` | Arıza sınıfı | Teknik Servis | Customer data, flat |
| `TaskPriority` | İş önceliği | On the issue | Normal / High / Urgent |
| `OutageClassification` | OOO / OOS | On blocking issue | Not RoomReadiness |
| `RoomServiceability` | Teknik elverişlilik | Derived by TS; consumed by Room Ops / FO | Not stored on Room |
| `PreparationImpact` | Hazırlık etkisi | Declared by TS; applied by Room Ops | None / RequiresPreparation |
| `RoomBlock` | Bloke | Ön Büro later | Out of this domain |
| `MaintenanceWorkOrder` | İş Emri (separate aggregate) | Future if needed | **Not** 0.11B |

Do not design EF navigations, repositories, or API endpoints in this record.

---

## Key decisions

| Topic | Choice |
|-------|--------|
| Boundary | Teknik Servis owns Arıza + derived serviceability; does not own Ready or Bloke |
| Issue vs WO | Option A — one `MaintenanceIssue` |
| Module name (when built) | Technical Service (`TechnicalService`) |
| OOO / OOS | Outage class on blocking issue; derived room view |
| Blocking | Explicit `BlocksRoomUse`; not implied by Open |
| Category | Configurable reference data |
| Severity | Deferred; priority only |
| Location | Room-only first slice |
| Assets | Room + description; no registry |
| 0.11B | [FIRST_SLICE.md](FIRST_SLICE.md) — not started |

---

## Consequences

### Positive

- Matches guest → Ön Büro → Teknik Servis → (cannot fix) → Ön Büro without building FO or Stay.
- Preserves Accepted Ready ≠ Sellable and OOO/OOS ≠ Dirty/Ready.
- Leaves assets/PM/common areas for later without a Room-down-only trap (non-blocking issues exist).
- Reuses Workforce without Position auth.

### Negative

- Hotels that toggle OOO on the rack with no ticket will need to create an Arıza (that is intentional).
- Resolver-declared preparation impact can be under-reported.
- Employee–User unlink means history shows the login user, not automatically the Employee.
- First-slice assignment may include any Active Employee until technician eligibility exists.

---

## Risks

| Risk | Mitigation |
|------|------------|
| 0.11B grows into CMMS | Explicit OUT list in FIRST_SLICE |
| OOO stored on Room, drifting from issues | Derived serviceability only |
| Bloke implemented as OOS | Boundary + invariants |
| Position-based “elektrikçi” routing | ADR-008; document eligibility limitation |
| Always-dirty-after-repair | S4 + PreparationImpact |
| Treating expert hotel as universal | Evidence labels; closed questions remain a baseline, not a survey |

---

## Revisit conditions

- Evidence that the hotel must file non-room faults in the first production slice
- Evidence that one defect routinely has multiple distinct work executions (then consider `MaintenanceWorkOrder`)
- A Stay/FO slice that must consume `TechnicalRoomChangeMayBeNeeded`
- Evidence that technician eligibility must be narrower than Active employment
- Evidence that Supervisor must review or override `PreparationImpact`

---

## Date

2026-08-23 (Sprint 0.11A)

---

## Related documents

- [README](README.md)
- [DOMAIN_BOUNDARY.md](DOMAIN_BOUNDARY.md)
- [ISSUE_MODEL.md](ISSUE_MODEL.md)
- [ROOM_SERVICEABILITY.md](ROOM_SERVICEABILITY.md)
- [WORKFLOW.md](WORKFLOW.md)
- [CROSS_DEPARTMENT_COORDINATION.md](CROSS_DEPARTMENT_COORDINATION.md)
- [INVARIANTS.md](INVARIANTS.md)
- [FIRST_SLICE.md](FIRST_SLICE.md)
- [ROOM-OPS-DOMAIN-001](../room-operations/ROOM-OPS-DOMAIN-001.md)
- [HR-DOMAIN-001](../hr/HR-DOMAIN-001-Organization-Workforce-Foundation.md)
