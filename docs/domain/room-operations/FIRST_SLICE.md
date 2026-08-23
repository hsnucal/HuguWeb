# First implementation slice — frozen Sprint 0.9B scope

> **Status:** Frozen intended direction for Sprint **0.9B**. Implementation notes: [SPRINT_0_9B_IMPLEMENTATION_NOTES.md](SPRINT_0_9B_IMPLEMENTATION_NOTES.md). This document is the accepted scope; it is not rewritten by the implementation.
>
> 0.9B must stay **small**. It proves Room Operations, not a Kat Hizmetleri platform.

## What 0.9B must prove

```text
Organization → Property → Room (minimal reference)
Room has one current RoomReadiness: Dirty | Clean | Inspected | Ready
HousekeepingWorkItem can be assigned to Employee
Supervisor inspection: accept → Inspected → Ready; reject → Dirty + required reason + rework
An operational view: which rooms need attention
```

Workforce already exists. Reuse `Employee`. Do not modify Workforce schema.

---

## IN 0.9B

- minimal Room reference
- RoomReadiness
- Dirty / Clean / Inspected / Ready
- HousekeepingWorkItem
- assign work to Employee
- basic work priority
- mark cleaning complete
- Supervisor inspection
- accept
- reject with required reason
- rework
- readiness history
- inspection history
- active room operations view
- manual “room needs cleaning” stand-in
- small development Room seed on existing Property
- TR / EN / RU UI
- permission-based authorization

### Notes on the stand-in

Until Stay exists, a manual **vacated / needs cleaning** business action may be used as a temporary operational stand-in. Do **not** name it a Checkout API if Checkout does not exist. Do not implement Reservations/Stay.

Priority: small first-level set (conceptual `Normal` / `High` / `Urgent`; naming may improve if justified). No scoring engine.

RoomType: do **not** add unless implementation later proves it unavoidable.

---

## OUT OF 0.9B

- Reservations
- Stay
- Occupancy
- RoomType
- Minibar
- Technical Service
- Out of Order
- Out of Service
- Blocked
- Sellability persistence
- DND
- No Service
- Lost & Found
- public-area cleaning
- generic operational tasks
- floor roster
- discrepancy reporting
- notifications
- chat
- WhatsApp replacement infrastructure
- mobile app
- passcards
- attendance
- shift planning
- inspection photos
- SGK / KBS
- broker
- outbox
- worker

---

## Why inspection stays in

**Inspection is in 0.9B** even though [MVP Candidates](../../product/MVP_CANDIDATES.md) warned against a full housekeeping platform including inspection programs. That warning still stands for *linen programs, task boards as a product, and mobile*. Inspection is in because Ready without Inspected would be a false machine. `Inspected` remains a real domain state even if a future FO UI collapses buckets.

**Blocked / OOO / OOS are in the conceptual model, out of 0.9B code.** Implementing them now would pull FO and TS into the first operations proof.

**Stayover** is a later capability of the same readiness machine. Occupancy/Stay are not implemented merely to support it.

---

## Future mobile consumers (not built now)

Documented so 0.9B web contracts do not paint the domain into a desktop-only corner.

| Persona (procedure, not Position enum) | Needs |
|----------------------------------------|--------|
| Kat Görevlisi | Assigned rooms, priority, later prep notes, mark clean, see rework |
| Supervisor | Inspection queue, accept/reject+reason, priority management |
| Minibar | Rooms awaiting control, confirm, differences |
| Teknik Servis | Open room issues, complete, unable-to-repair |
| Ön Büro | Readiness visibility, later sellability, priority conflicts |

No mobile framework selection.

---

## Audit facts 0.9B should persist as business data

- who marked Clean, when
- who inspected, when, accept vs reject
- rejection reason
- who assigned/reassigned
- who changed priority

Do not treat API logs as this record.

---

## Explicit non-goals (repeat)

No production authorization in 0.9A. 0.9B, if started later, is still not: chat, PMS, HK ERP, maintenance CMMS, Reservations, or a generic workflow engine.
