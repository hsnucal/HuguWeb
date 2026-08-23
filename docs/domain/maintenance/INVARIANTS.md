# Invariants — Teknik Servis

> **Status:** Accepted — Product Owner + CTO approved reference baseline. Sprint 0.11A. Classification uses HuGuWeb evidence language.

**CONFIRMED / ACCEPTED** — Product Owner + CTO accepted this as HuGuWeb’s Technical Service reference baseline (E2 proxy for this discovery context, plus Accepted Room Operations / Workforce and E0–E1 reference model). Binding for the Teknik Servis model unless a later hotel contradicts it. Not a claim every hotel is identical.

**ACCEPTED PRODUCT CONTEXT** — Binding Room Operations / Workforce / ADR facts this domain must not contradict.

**EXPERT-SUPPLIED WORKFLOW** — Product Owner / hotel expert process for this discovery context.

**REFERENCE MODEL** — Hospitality + HuGuWeb architecture defaults.

---

## Ownership and dimensions

| ID | Invariant | Class |
|----|-----------|-------|
| M1 | Teknik Servis does not write `RoomReadiness` (`Dirty` / `Clean` / `Inspected` / `Ready`). | **CONFIRMED / ACCEPTED** |
| M2 | OOO / OOS are not RoomReadiness values and are not **Bloke**. Bloke is Ön Büro later. | **CONFIRMED / ACCEPTED** |
| M3 | `Ready` ≠ technical serviceability ≠ Sellable. Sellable is not a stored master status in this domain. | **CONFIRMED / ACCEPTED** |
| M4 | `HousekeepingWorkItem` is not used for technical work. No generic `OperationalTask<T>`. | **CONFIRMED / ACCEPTED** |
| M5 | Assignment targets `EmployeeId`. Position / Department names never grant permission and are never matched for eligibility. | **CONFIRMED / ACCEPTED** |

---

## Issue

| ID | Invariant | Class |
|----|-----------|-------|
| I1 | First slice: one aggregate `MaintenanceIssue`. No `MaintenanceWorkOrder` root. | **CONFIRMED / ACCEPTED** |
| I2 | First slice: issue is property-scoped and **requires** `RoomId` (existing Room Operations Room). | **CONFIRMED / ACCEPTED** |
| I3 | Category is customer-defined property-scoped reference data, flat, not a hard-coded C# trade enum, and not a hierarchy. | **CONFIRMED / ACCEPTED** |
| I4 | Priority is `Normal` / `High` / `Urgent`. No severity score in 0.11B. | **CONFIRMED / ACCEPTED** |
| I5 | `Assigned` is not a status. `InProgress` requires `AssignedEmployeeId` of an Employee with Active employment. | **CONFIRMED / ACCEPTED** |
| I6 | Accepted paths: `Open` → `InProgress` → `Resolved`, or `Open` → `InProgress` → `UnableToResolve` → `InProgress` → `Resolved`. Do not add `Closed`. | **CONFIRMED / ACCEPTED** |
| I7 | `Resolved` is terminal for that issue. Recurrence is a new Arıza. | **CONFIRMED / ACCEPTED** |
| I8 | `UnableToResolve` is not `Resolved` and is **not** terminal. Required note. Same issue may resume to `InProgress`. | **CONFIRMED / ACCEPTED** |
| I9 | Resolution note is required for `Resolved` and `UnableToResolve`. Photo is not required. TemporaryFix is out. | **CONFIRMED / ACCEPTED** |
| I10 | Reporter is optional `EmployeeId` plus optional origin note — not ApplicationUser-only, not a department enum. | **CONFIRMED / ACCEPTED** |
| I11 | First slice may assign any Active Employee. Safe technician eligibility does not yet exist. | **CONFIRMED / ACCEPTED** (limitation) |

---

## Serviceability

| ID | Invariant | Class |
|----|-----------|-------|
| S1 | An open issue does **not** automatically make the oda technically unavailable. | **CONFIRMED / ACCEPTED** |
| S2 | Technical unavailability requires at least one issue with `BlocksRoomUse` in `Open`, `InProgress`, or `UnableToResolve`. | **CONFIRMED / ACCEPTED** |
| S3 | `OutageClassification` (`OutOfOrder` / `OutOfService`) is required exactly when `BlocksRoomUse` is true; otherwise null. | **CONFIRMED / ACCEPTED** |
| S4 | Current room serviceability is **derived**, not a TS-owned master column on `Room`. Display classification: any active blocking OOS → `OutOfService`; else any active blocking OOO → `OutOfOrder`; else `Serviceable`. | **CONFIRMED / ACCEPTED** |
| S5 | Same-day vs not (OOO vs OOS) is operator classification, not a clock/SLA rule. | **CONFIRMED / ACCEPTED** |
| S6 | Closing the last blocking issue restores technical serviceability. It does not set Ready. | **CONFIRMED / ACCEPTED** |
| S7 | Checkout / vacated does not clear Teknik Servis issues. | **CONFIRMED / ACCEPTED** |
| S8 | Multiple blocking issues may exist for one Room. Do not enforce a single blocker. | **CONFIRMED / ACCEPTED** |
| S9 | `UnableToResolve` does not restore serviceability if the issue is blocking. Guest movement / room change does not resolve the fault. | **CONFIRMED / ACCEPTED** |
| S10 | Ön Büro / a reporting user may initially set `BlocksRoomUse`. Teknik Servis may validate or change it. Authority is not encoded by department or Position strings. | **CONFIRMED / ACCEPTED** |

---

## Room Operations integration

| ID | Invariant | Class |
|----|-----------|-------|
| R1 | Repair does not universally dirty the oda. `PreparationImpact = None` must not reset readiness. | **CONFIRMED / ACCEPTED** |
| R2 | `PreparationImpact = RequiresPreparation` is a consume contract for Room Operations, not a TS write of readiness. | **CONFIRMED / ACCEPTED** |
| R3 | First slice: the resolving Technical Service operator declares `PreparationImpact` when setting `Resolved`. | **CONFIRMED / ACCEPTED** |
| R4 | If `RequiresPreparation` and the Room is already Dirty with an appropriate active housekeeping work item, reuse that flow; do not create duplicate cleaning work. If no preparation cycle exists, Room Operations starts the appropriate preparation / needs-cleaning behavior. | **CONFIRMED / ACCEPTED** |
| R5 | Integration is thin in-process only. No broker, outbox, or worker. | **CONFIRMED / ACCEPTED** |

---

## History

| ID | Invariant | Class |
|----|-----------|-------|
| H1 | Create, category change, priority change, assignment, start, blocking/OOO-OOS change, resolve, unable-to-resolve, resume, preparation impact, and serviceability restoration are **business history** on the issue — not application logs. | **CONFIRMED / ACCEPTED** |
| H2 | Do not build generic audit infrastructure for this domain. | **CONFIRMED / ACCEPTED** |
| H3 | Actor may be `ApplicationUser` for now because Employee/User linking is not implemented. | **CONFIRMED / ACCEPTED** |

---

## Authorization

| ID | Invariant | Class |
|----|-----------|-------|
| A1 | 0.11B permission set is `maintenance.read`, `maintenance.manage`, `maintenance.resolve`. Do not create `maintenance.report`. | **CONFIRMED / ACCEPTED** |
| A2 | Do not create runtime authorization roles such as Technician or MaintenanceManager. Authorization is permission claims only. | **CONFIRMED / ACCEPTED** |

---

## Deliberately not invariants yet

- Common-area / asset identity required to file an Arıza
- TemporaryFix outcome
- Reopen-from-Resolved
- At most one blocking issue per room (derive rule handles more than one)
- Employee must have a login
- Technician eligibility by Position/Department
- Bloke or oda değişikliği inside this domain
- Clock-based promotion OOO → OOS at midnight
- Supervisor override of `PreparationImpact`
