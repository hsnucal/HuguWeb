# First implementation slice — frozen Sprint 0.11B scope

> **Status:** Accepted recommended direction for Sprint **0.11B**. This document does **not** start 0.11B.
>
> 0.11B must stay **small**. It proves Teknik Servis coordination, not a CMMS.

## What 0.11B must prove

```text
Organization → Property → Room (already exists)
MaintenanceIssue can be created on a Room
Priority + category (reference data) + assignment to Active Employee
Blocking / non-blocking; blocking has OutOfOrder | OutOfService
Open → InProgress → Resolved
or Open → InProgress → UnableToResolve → InProgress → Resolved
Derived room technical serviceability
Resolution note + preparation-impact declaration
Issue business history
An operational view: which technical work needs attention
Thin in-process consume: RequiresPreparation → Room Operations hazırlık path
```

Workforce and Room identity already exist. Reuse `Employee` and `Room`. Do not modify Workforce schema. Do not move Room identity.

---

## IN 0.11B

- Room-linked `MaintenanceIssue` (`RoomId` required)
- property-scoped `MaintenanceIssueCategory` reference (flat list; seed illustrative names; customer configurable later — not a category admin product)
- create issue
- description
- priority `Normal` / `High` / `Urgent`
- assign / reassign Active `Employee` (any Active Employee; technician eligibility does not yet exist)
- `BlocksRoomUse`
- OOO / OOS when blocking
- `Open` / `InProgress` / `UnableToResolve` / `Resolved`
- start work (`Open` → `InProgress`; assignee required)
- resolve with required note
- unable-to-resolve with required note
- resume `UnableToResolve` → `InProgress`
- `PreparationImpact` on Resolve (`None` / `RequiresPreparation`)
- derived Room Technical Serviceability
- issue history
- active Technical Service work view
- TR / EN / RU
- permission-based authorization: `maintenance.read`, `maintenance.manage`, `maintenance.resolve`
- thin in-process Room Operations integration for `RequiresPreparation`

Reported-by Employee + origin note: **in** (small fields).

Manual create from the TS (and permissioned users) is enough. No Stay trigger. Do **not** create `maintenance.report` yet.

---

## OUT OF 0.11B

- common areas
- Location aggregate
- Asset
- equipment registry
- preventive maintenance
- scheduled maintenance
- spare parts
- inventory consumption
- purchasing
- vendors
- SLA
- maintenance cost
- photos
- TemporaryFix
- reopen after Resolved
- separate WorkOrder aggregate
- notifications
- SignalR
- broker
- outbox
- worker
- mobile
- QR
- IoT
- Reservations
- Stay
- Front Office implementation
- Room Change
- Bloke implementation
- sellability persistence
- team aggregate
- Position/Department eligibility engine
- `maintenance.report` split
- runtime roles such as Technician or MaintenanceManager
- generic operational task platform
- changing Room Operations readiness machine (Dirty/Clean/Inspected/Ready values)

---

## Why blocking + OOO/OOS stay in

Without blocking vs non-blocking, every paint scratch would take an oda out of sale. Without OOO vs OOS, Ön Büro loses the expert duration distinction they already use. Both stay **on the issue**, derived for the room — not RoomReadiness.

**Bloke stays out of code** (FO later), same as 0.9B.

---

## Why Room Operations consume is in

The critical invariant is: repair does not always dirty the oda, and TS does not own Ready. Recording `PreparationImpact` without applying `RequiresPreparation` would not prove the integration. The consume must be a **small in-process call**, not a broker.

If the Room is already Dirty with an appropriate active housekeeping work item, reuse that flow. If no preparation cycle exists, Room Operations starts the appropriate preparation / needs-cleaning behavior.

If 0.11B is later split further, the fallback is: persist the declaration and defer the consume — that would be an incomplete vertical slice.

---

## Authorization (recommended, not implemented in 0.11A)

Smallest permission set:

| Permission | Intent |
|------------|--------|
| `maintenance.read` | See issues, history, derived serviceability |
| `maintenance.manage` | Create, categorize, priority, assign, blocking/OOO-OOS, start work |
| `maintenance.resolve` | Resolved / UnableToResolve + notes + preparation impact |

Do **not** create `maintenance.report` yet. Do **not** create runtime authorization roles such as Technician or MaintenanceManager. Development personas may be added later for testing; authorization remains permission claims only.

Mirror existing style (`workforce.*`, `room-operations.*`).

---

## Conceptual relational model (no SQL, no EF)

Existing:

```text
Organization → Property → Room
Employee (Active employment required for new assignment)
```

Accepted conceptual model:

```text
Property 1—* MaintenanceIssueCategory
Property 1—* MaintenanceIssue
Room 1—* MaintenanceIssue          (required RoomId)
Employee 0..1 ← MaintenanceIssue.AssignedEmployeeId
Employee 0..1 ← MaintenanceIssue.ReportedByEmployeeId
MaintenanceIssue 1—* MaintenanceIssueHistory
```

**No** `MaintenanceWorkOrder` table. **No** `Room.Serviceability` column. **No** `Asset` table.

`MaintenanceIssue` holds: category, description, priority, status, assignment, `BlocksRoomUse`, `OutageClassification`, reporter, origin note, resolution note, preparation impact, timestamps.

`MaintenanceIssueHistory` holds: occurred at, acting user id (Identity / `ApplicationUser` for now), event type, from/to payload, optional note.

---

## Future mobile consumers (not built)

| Procedure (not Position enum) | Needs |
|-------------------------------|--------|
| Technician | Assigned Arıza, oda, description, priority, start, resolve, unable, preparation impact |
| TS management | Unassigned / Urgent / blocking queue |
| Ön Büro | Blocking rooms, çözülemedi, later oda değişikliği cue |

No mobile framework selection.
