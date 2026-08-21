# Room Readiness — Kirli → Temiz → Denetimli → Hazır

> **Status:** Accepted — Product Owner + CTO approved baseline.
>
> Product names: **Kirli / Temiz / Denetimli / Hazır**.  
> Technical: `Dirty` / `Clean` / `Inspected` / `Ready`.

## 1. What this machine is

This is **one preparation state machine**.

It answers: *Oda temizlik ve denetim açısından nerede?*

Typical trigger: the room needs housekeeping work after checkout / vacated.

It does **not** encode:

- occupancy
- Blocked
- OOO / OOS
- DND / No Service
- Minibar
- sellability

Those are other dimensions ([ROOM_MODEL.md](ROOM_MODEL.md)).

---

## 2. States

| Technical | Turkish | Meaning |
|-----------|---------|---------|
| `Dirty` | Kirli | Needs cleaning (checkout, vacated, rejected inspection, or equivalent). |
| `Clean` | Temiz | Kat Görevlisi marked cleaning complete. **Not** inspected. **Not** Ready. |
| `Inspected` | Denetimli | Supervisor accepted the physical inspection. **A real domain readiness state.** |
| `Ready` | Hazır | Preparation path complete. Still **not** automatically sellable. |

**Clean does not imply Inspected. Checkout / vacated cannot directly make the room Ready. Ready does not imply Sellable.**

`Inspected` must **not** be removed merely because a future Front Office UI may show only Dirty / Clean / Ready. Domain fidelity takes precedence over simplified UI buckets. A FO summary view may collapse Inspected+Ready for **display** later; the domain still has four readiness values.

---

## 3. Transitions and who may cause them

Distinguish **domain actor** (procedure) from **Position** (Workforce data) from **permission** (ADR-008).

Position names such as “Kat Görevlisi” must **never** grant application rights. Permissions are assigned to users.

| Transition | Domain actor (procedure) | Permission idea (not implemented) |
|------------|--------------------------|-----------------------------------|
| → `Dirty` | Checkout / vacated fact; inspection rejection; (later) other dirtied events | System/contract + inspect-reject |
| `Dirty` → `Clean` | Person completing cleaning work | complete-cleaning |
| `Clean` → `Inspected` | Supervisor accepting inspection | inspect-accept |
| `Inspected` → `Ready` | Same acceptance path (may be one command that records Inspected then Ready) | inspect-accept |
| `Clean`/`Inspected` → `Dirty` | Supervisor rejection | inspect-reject |

Checkout **cannot** move a room to Ready.

Joker coverage: any Employee who is **assigned the work item** may complete cleaning if they hold the permission — not because their Position title is Kat Görevlisi.

Order Taker is **not** modeled as a readiness state actor in the first slice.

---

## 4. Inspection and rejection

Supervisor **physical inspection** is part of the first true readiness flow.

Accepted path:

```text
Clean → Supervisor inspection → Inspected → Ready
```

| Outcome | Effect |
|---------|--------|
| Accept | Readiness proceeds `Inspected` → `Ready`. Inspection record is kept. |
| Reject | Readiness returns to `Dirty`. Reason/comment is **required**. Photo is **not** required. Rework returns to the **appropriate assigned Kat Görevlisi** when applicable (reassignment allowed). Priority **may** increase. |

Inspection history is retained. A later accept does not erase the rejection.

---

## 5. State vs work item

**“Oda 214 Kirli”** is readiness state.  
**“214’ü temizle işi”** is a `HousekeepingWorkItem`.

They are related, not identical:

- State can exist before a person is assigned.
- Work can be reassigned without changing Dirty.
- Completed/cancelled work must **not** overwrite a newer readiness (stale completion).
- Extra guest requests (extra towel) are often work **without** changing the Dirty→Ready machine.

Sprint 0.9B should eventually implement **both** concepts. A state-only model cannot represent assignment, joker coverage, or rework targeting. A task-only model without current state forces Ön Büro to infer house status from open tickets.

Do **not** create a generic workflow engine.

See [HOUSEKEEPING_OPERATIONS.md](HOUSEKEEPING_OPERATIONS.md).

---

## 6. Checkout trigger (conceptual)

Future Stay/Ön Büro emits a fact, for example:

```text
StayCheckedOut { propertyId, roomId, occurredAt, stayId }
```

Room Operations **consumes** it:

1. Set readiness to `Dirty`.
2. Open (or reopen) a `HousekeepingWorkItem`.
3. Later: may open Minibar check (not 0.9B).
4. Existing Teknik Servis issues remain visible; checkout does not clear them.

No message broker, outbox, or worker is authorized. This is a **contract** for a future in-process call or application event.

Sprint 0.9B must **not** implement Reservations/Stay. Until Stay exists, 0.9B may allow a **manual operational “mark vacated / needs cleaning”** action as a temporary stand-in. Do **not** name it a Checkout API if Checkout does not exist.

---

## 7. Yesterday’s Ready rooms

**Ready does not automatically expire every morning.**

Ready remains until a real domain event invalidates readiness (for example vacated/needs cleaning, inspection rejection, or a later dirtied event).

A hotel’s daily revalidation procedure may later create a **separate** inspection/revalidation work item. Do **not** add automatic Ready reset.

---

## 8. Stayover cleaning

The model should be **capable** of supporting stayover cleaning later (same readiness machine; occupancy independent: Occupied + Dirty is valid).

Sprint 0.9B remains **checkout/vacated-centric**. Do **not** implement Occupancy or Stay merely for stayover support.

---

## 9. What readiness does not own

- Bloke, OOO, OOS
- DND / Hizmet İstemiyor
- Minibar confirmation (including minibar-pending)
- Folio / charging
- Reservation notes (consumes a future preparation-requirements view only)
