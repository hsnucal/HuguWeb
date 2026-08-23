# Cross-department coordination

> **Status:** Accepted — Product Owner + CTO approved baseline.
>
> HuGuWeb direction: **recorded domain actions** instead of phone / WhatsApp / radio where practical. **Not** a chat product.

## 1. Coordination style

Prefer:

- status transitions
- assignments
- inspection accept/reject with reason
- later: notifications **from those facts**

Do **not** design a messenger. Do **not** implement notification infrastructure in 0.9A/0.9B.

### Facts that should later notify (conceptual)

| Fact | Typical consumers |
|------|-------------------|
| Room became `Dirty` after checkout / vacated | Kat Hizmetleri, assigned attendant |
| Cleaning completed (`Clean`) | Supervisor inspection queue |
| Inspection accepted (`Ready`) | Ön Büro |
| Inspection rejected | Assigned attendant, HK management |
| Priority changed by FO or HK management | Assigned attendant |
| Minibar difference / confirmation (later) | Ön Büro |
| Technical unable-to-repair (later) | Ön Büro (possible room change) |
| Repair completed (later) | Kat Hizmetleri / Supervisor only if preparation is affected |
| Blocked / unblocked (later) | Ön Büro, management |

Until a notification slice exists, the **web Operations Center / room list** is the coordination surface.

---

## 2. Ön Büro (Front Office)

**Needs:** visibility of operational readiness. Inspection waiting remains in the domain even if a later FO UI shows Dirty / Clean / Ready buckets.

**May:** set business priority when urgent arrivals compete.

**Will own later:** check-in/out, room assignment to guest, **Blocked**.

**Must not:** mark rooms Ready without inspection path; own cleanliness; persist Blocked inside RoomReadiness.

**0.9B:** a room operations view usable by FO-permissioned users. No reservation, check-in, checkout, or Blocked persistence.

Keep the conceptual rule documented: **Ready ≠ Sellable**.

---

## 3. Minibar

Dedicated Minibar function exists in some hotels. Checkout may compare expected vs actual products. Some properties **wait** for minibar before checkout completes; others do not.

### Boundary decision

Minibar is **out of Sprint 0.9B**.

Minibar confirmation is **not** a `RoomReadiness` value. Even if some hotels wait for Minibar:

- Minibar remains a **parallel operational gate**
- it is **not** a RoomReadiness state
- do **not** implement minibar-pending in 0.9B

| Hotel policy | Effect |
|--------------|--------|
| Checkout waits for minibar | Sellability/checkout completion gated by minibar confirmation — **not** by renaming Ready |
| Checkout does not wait | Minibar work is parallel; does not block Dirty→Ready |

Do **not** implement billing, Folio, or inventory now.

---

## 4. Teknik Servis (Technical Service)

Technical Service remains **out of Sprint 0.9B**.

Flow today: guest → Ön Büro creates issue → Teknik Servis. If unrepairable → inform Ön Büro → possible room change.

Room Operations **does not** own the technical-issue lifecycle. Out of Order / Out of Service are future serviceability concerns, **not** RoomReadiness states.

Sprint 0.11A **Accepted** the Teknik Servis domain ([docs/domain/maintenance/](../maintenance/README.md)). First slice uses one `MaintenanceIssue` aggregate. Room Operations consumes **derived** `RoomServiceability` (blocking Arıza → OOO/OOS vs available). Do not encode OOO/OOS as readiness. Sprint 0.11B is **not** started.

Technical repair does **not** universally require Supervisor inspection.

- If the intervention affects room preparation/readiness, a new preparation/inspection cycle may be required.
- Otherwise repair alone should **not** automatically dirty the room.

| Later TS owns | Room Operations consumes |
|---------------|--------------------------|
| Issue, diagnosis, completion, unable-to-repair | `RoomServiceability` effect on sellability; issue still visible after checkout |

Repair completion ≠ Ready ≠ Sellable.

**0.9B:** no Technical Service module. Do not encode OOO/OOS as readiness.

---

## 5. Reservation / Stay / Checkout

Do **not** implement Reservations / Stay in Sprint 0.9B. Do not design the Reservation domain in this sprint beyond the consume contract.

**Consume later:**

- `StayCheckedOut { propertyId, roomId, occurredAt, stayId }` → Dirty + cleaning work
- expected arrival time → priority
- preparation notes (baby bed, extra towels, honeymoon, accessibility, sofa bed, pillows) → extra work or notes on the room’s work
- “guest already checked in” → skip/defer physical vacant inspection

Room Operations never stores the reservation as a booking.

No broker/outbox/worker.

Until Stay exists, use a manual **vacated / needs cleaning** business action as a temporary operational stand-in if required. Do **not** name it a Checkout API if Checkout does not exist.

---

## 6. Lost & Found (Kayıp Eşya)

Current: finder fills a form, delivers to Order Taker. Future: record, optional photo, location, time, finder, unique 6-digit reference; Reception / Guest Relations / Security search.

**Boundary:** separate future capability. Not Room Readiness. May reference a Room as **found location**.

**0.9B:** out.

Order Taker remains a **Position** (customer-defined) and a **coordination role in today’s hotel**, not a HuGuWeb module and not a hard-coded permission.

---

## 7. DND / No Service

Treat **Rahatsız Etmeyin (Do Not Disturb)** and **Hizmet İstemiyor (No Service)** as **separate future guest-service restrictions**.

Do **not** implement either in Sprint 0.9B. Do **not** place them in RoomReadiness. They are also not simply Occupancy.

| Concept | Meaning (working) |
|---------|-------------------|
| DND | Do not enter / do not disturb — typically blocks HK entry |
| No Service | Guest declined housekeeping service (may be verbal via FO/Guest Relations) |

They may originate as stay preference **or** current-day instruction. They can **pause or skip** a housekeeping work item without making the room Ready.

Exact distinction (forbidden entry vs skip cleaning) remains hotel-specific later detail. First slice: out.

---

## 8. Passcards / access

Desired: shift start → floor/room access active; shift end → access expires.

Depends on Workforce + Attendance/Shift + Access Control.

**Out of implementation.** Room Operations is a **future consumer** of “this Employee is on duty for these rooms,” not the access-control system.

---

## 9. Occupancy discrepancy

End-of-day: FO recorded occupancy vs Supervisor observed occupancy → FO / Guest Relations / Accounting.

Useful future **comparison report**. Not a reason to make Supervisor the owner of Occupancy.

**0.9B:** out.

---

## 10. Government / legal

No SGK, Police KBS, Jandarma KBS, identity reporting, or e-Invoice in this domain.
