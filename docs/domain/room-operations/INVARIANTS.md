# Invariants — Oda Operasyonları

> **Status:** Accepted — Product Owner + CTO approved baseline. Classification uses HuGuWeb evidence language.

**CONFIRMED / ACCEPTED** — Product Owner + CTO accepted this as HuGuWeb’s first operations baseline (E2 proxy for this discovery context, plus E0–E1 reference model). Binding for the Room Operations model unless a later hotel contradicts it. Not a claim every hotel is identical.

**REFERENCE-MODEL** — Standard hospitality practice + accepted HuGuWeb product/architecture direction. Safe as the default model; not a survey of all hotels.

---

## Readiness and inspection

| ID | Invariant | Class |
|----|-----------|-------|
| R1 | Checkout / vacated cannot directly make a room `Ready`. | **CONFIRMED / ACCEPTED** |
| R2 | `Clean` ≠ `Inspected`. Supervisor physical inspection is a distinct step. | **CONFIRMED / ACCEPTED** |
| R3 | Inspection reject returns the room to `Dirty`. Rejection reason/comment is required. Photo is not required. | **CONFIRMED / ACCEPTED** |
| R4 | Rework returns to the appropriate assigned Kat Görevlisi when applicable (typically the assigned Employee; reassignment allowed). | **CONFIRMED / ACCEPTED** |
| R5 | Inspection history is retained; later accept does not erase rejections. | **CONFIRMED / ACCEPTED** |
| R6 | There is one **current** readiness value per room. | **REFERENCE-MODEL** |
| R7 | Inactive/ended work items must not overwrite newer readiness (e.g. a late “clean” after a new checkout). | **REFERENCE-MODEL** |
| R8 | Ready does not automatically expire. Ready persists until a real domain event invalidates it. Do not add automatic morning Ready reset. A later hotel revalidation procedure may create a separate work item. | **REFERENCE-MODEL** |
| R9 | Stayover cleaning can later use the same readiness machine while occupancy is independent (`Occupied` + `Dirty` is valid). Sprint 0.9B remains vacated-centric and does not implement Occupancy/Stay for stayover. | **REFERENCE-MODEL** (later capability); **0.9B OUT accepted** |
| R10 | `Inspected` is a real domain readiness state. A future FO UI may show Dirty / Clean / Ready as display buckets; that does not remove Inspected from the domain. | **CONFIRMED / ACCEPTED** |

---

## Sellability and other dimensions

| ID | Invariant | Class |
|----|-----------|-------|
| S1 | `Ready` ≠ Sellable. Ready is preparation complete. Sellable is composed later from independent conditions. | **CONFIRMED / ACCEPTED** |
| S2 | Operational **Blocked** is independent of readiness. A Ready room may be Blocked. Do not persist Blocked in 0.9B. | **CONFIRMED / ACCEPTED** |
| S3 | Technical **Out of Order** and **Out of Service** are independent of readiness and of Blocked. They are not RoomReadiness values. Do not implement them in 0.9B. | **CONFIRMED / ACCEPTED** |
| S4 | Technical repair does not universally require Supervisor inspection. If the intervention affects room preparation/readiness, a new preparation/inspection cycle may be required. Otherwise repair alone should not automatically dirty the room. Technical Service is out of 0.9B. | **REFERENCE-MODEL** |
| S5 | Minibar is not `RoomReadiness`. Even if some hotels wait for Minibar, it remains a parallel operational gate. Do not implement minibar-pending in 0.9B. | **CONFIRMED / ACCEPTED** |
| S6 | DND / No Service (Rahatsız Etmeyin / Hizmet İstemiyor) are separate future guest-service restrictions, not `RoomReadiness`. Out of 0.9B. | **CONFIRMED / ACCEPTED** |

---

## Work, priority, workforce

| ID | Invariant | Class |
|----|-----------|-------|
| W1 | `RoomReadiness` and `HousekeepingWorkItem` are separate concepts. A room can be Dirty with no assignee yet. | **CONFIRMED / ACCEPTED** |
| W2 | Position ≠ permission. Kat Görevlisi must not automatically gain authority to override business-critical priority. HK management can change priority. Ön Büro may set business priority when urgent rooms conflict. | **CONFIRMED / ACCEPTED** |
| W3 | Priority changes should be auditable. | **REFERENCE-MODEL** |
| W4 | Assignment/reassignment history should remain. | **REFERENCE-MODEL** |
| W5 | Work assignment references `EmployeeId`. Position/Department names never grant permissions. | **CONFIRMED / ACCEPTED** |
| W6 | Kat Görevlisi / Supervisor / Minibar Görevlisi / Order Taker are organizational concepts, not hard-coded authorization roles. | **CONFIRMED / ACCEPTED** |
| W7 | Daily Employee → Floor roster is a future Kat Hizmetleri capability. First slice: `HousekeepingWorkItem` → `EmployeeId` is sufficient. | **CONFIRMED / ACCEPTED** (0.9B OUT) |

---

## Identity and scope

| ID | Invariant | Class |
|----|-----------|-------|
| I1 | Room is property-scoped (`Organization` → `Property` → `Room`) and identified independently of Stay. Reservations do not own physical Room identity. | **REFERENCE-MODEL** |
| I2 | Room Operations does not own Attendance, Shift, Payroll, Folio, or government reporting. | **CONFIRMED / ACCEPTED** (existing product/HR decisions) |

---

## Deliberately not invariants

These are closed as **out of Sprint 0.9B** or as hotel-specific later policy, not as frozen universal rules:

- Ready expires at a fixed clock time — **rejected** as an automatic rule.
- Minibar always blocks checkout — hotel policy; still not a readiness state.
- Supervisor org-chart is required to inspect — authorization, not Position.
- Public-area work is a Room.
- Exact OOO vs OOS “same day” clock vs operator judgment — independence of the dimension is accepted; timing labels may vary by hotel.
- Exact DND vs No Service entry/skip semantics — separate restriction dimension later; not readiness.
