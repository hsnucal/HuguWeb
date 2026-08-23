# Room Model — identity and operational dimensions

> **Status:** Accepted — Product Owner + CTO approved baseline. Product language: **Oda**. Technical identity: `Room`.

## 1. Room identity ownership

HuGuWeb does not yet have a Rooms, Inventory, or Reservation domain. Operations cannot wait for those to exist before attaching readiness to a real oda.

### First host (Sprint 0.9)

**Room Operations hosts a minimal Room reference.**

```text
Organization → Property → Room
```

`Property` already exists in Workforce ([ORGANIZATION_MODEL.md](../hr/ORGANIZATION_MODEL.md)). Do not duplicate Property. Room is property-scoped.

Reservations / Stay will consume `RoomId` later. They do **not** own Room identity.

### What Room Operations must own (minimal)

| Fact | Why |
|------|-----|
| Stable technical id | Cross-domain reference |
| Property id | Operating hotel |
| Room number / code | Staff language (“214”) |
| Active / inactive | Ended rooms must not receive new work |

Optional on first slice if needed for assignment: **floor** (`Floor`) as a simple attribute — not a Floor aggregate.

### What we do **not** automatically create

**RoomType** is a commercial/inventory concept (rate, occupancy, selling). Kat Hizmetleri may later *consume* room type for priority or linen. Do **not** add RoomType in Sprint 0.9B unless implementation later proves it unavoidable.

Connecting rooms, suites, non-room inventory, and public-area “rooms” are out.

### Future ownership (avoid circularity)

| Future domain | Owns | Consumes |
|---------------|------|----------|
| **Room Operations** (now) | Physical/operational Room reference; readiness; HK work | Stay checkout fact; later serviceability and block |
| **Reservations / Stay** (later) | Booking, assignment of a guest to a Room for dates, checkout | Room identity; readiness/sellability for check-in |
| **Inventory / Rooms catalog** (later, if split) | Commercial RoomType, sellable inventory rules | Room identity |
| **Teknik Servis** (later) | `MaintenanceIssue` / Arıza | Room identity |
| **Minibar** (later) | Expected/actual minibar contents, confirmation | Room identity; checkout fact |

Reservations must **not** own the physical Room. Room Operations must **not** own Stay.

If a later Rooms catalog module is justified, Room identity can move there; Room Operations would then reference it by id. That is an ownership transfer, not a cycle.

---

## 2. Do not explode a single RoomStatus

A giant enum mixing cleanliness, occupancy, technical state, FO block, and guest restrictions is **rejected**.

Do **not** combine into one enum:

Dirty, Clean, Inspected, Ready, Occupied, Vacant, Blocked, OutOfOrder, OutOfService, DND, NoService.

Those facts are **not mutually exclusive**.

Example that a single enum cannot represent honestly:

- Oda **Hazır** (Ready) **and** Ön Büro **Bloke** (Blocked) → physically ready, not sellable.
- Oda **Temiz** (Clean) **and** Teknik **Hizmet dışı** (Out of Service) → clean, not sellable.
- Oda **Kirli** (Dirty) **and** **Dolu** (Occupied) — stayover cleaning (if the hotel does it).
- Oda **Hazır** **and** **Rahatsız Etmeyin** — restriction on entering, not a cleanliness value.

---

## 3. Operational dimensions

The domain must distinguish conceptually:

| Dimension | Turkish product name | Technical | Owner | First slice |
|-----------|----------------------|-----------|-------|-------------|
| **A. Readiness** | Oda hazırlık | `RoomReadiness` | Room Operations | **Yes** — the only dimension entering Sprint 0.9B |
| **B. Occupancy** | Doluluk | `Occupancy` (future) | Stay / Ön Büro | **No** |
| **C. Technical serviceability** | Teknik elverişlilik | `RoomServiceability` | Teknik Servis owns issues; Room Operations consumes effect | **No** |
| **D. Operational block** | Operasyonel blok | `RoomBlock` | Ön Büro | **No** — do not persist Blocked in 0.9B |
| **E. Guest service restriction** | Hizmet kısıtı | `RoomServiceRestriction` | Stay/guest request + current restriction | **No** |
| **F. Sellability** | Satışa uygunluk | `Sellability` | **Derived** later | Documented; not stored as the source of truth |

Cleanliness, inspection, and “Ready” are **one readiness progression**, not three independent stored dimensions. See [READINESS_MODEL.md](READINESS_MODEL.md).

---

## 4. Ready vs Sellable

**Ready ≠ Sellable.**

| Term | Meaning |
|------|---------|
| **Ready** (`Ready`) | Room preparation process completed (clean + inspected path). |
| **Sellable** | Room may be assigned/sold **now**, considering multiple independent conditions. |

A room may be Ready and still not Sellable if, for example:

- Ön Büro **Blocked** (payment/contact lock)
- Teknik **Out of Order** or **Out of Service**
- Occupied (already assigned/in-house)
- other hotel-specific gates (e.g. minibar confirmation)

Sellability is a **composition**, not a fifth value on the Dirty→Ready machine. Do **not** store Sellable as a master status in Sprint 0.9B.

Sprint 0.9B should display **readiness**, not pretend to be the full sellability engine.

---

## 5. Blocked / Out of Order / Out of Service

These are **not** one enum and **not** RoomReadiness states. Do not implement any of them in 0.9B.

| Concept | Turkish | Who decides | Meaning (PO workflow) |
|---------|---------|-------------|------------------------|
| **Out of Order** | Aynı gün giderilmesi beklenen arıza (OOO) | Teknik Servis + FO coordination | Future Technical Service / serviceability concern |
| **Out of Service** | Aynı gün giderilemeyecek arıza (OOS) | Teknik Servis + FO coordination | Future Technical Service / serviceability concern |
| **Blocked** | Bloke | Ön Büro | Front Office-owned operational lock — **not** a technical defect and **not** readiness |

After technical repair, the room may still need Kat Hizmetleri / Supervisor before Ready. Repair completion ≠ Ready ≠ Sellable.

“Same day” is operator judgment in the supplied workflow, not a clock rule. Exact codes/labels may vary by hotel; the **independence** of the dimension is the model.

Sprint 0.11A **Accepted** how Teknik Servis stores these facts: OOO/OOS as outage classifications on a **blocking** `MaintenanceIssue`; current room serviceability **derived**, not a master column on `Room`. See [ROOM_SERVICEABILITY.md](../maintenance/ROOM_SERVICEABILITY.md). That refinement does **not** change the Accepted facts that OOO/OOS are independent of RoomReadiness and of Blocked, or that Room Operations does not own them.

---

## 6. Occupancy

Occupancy is a **Stay/Ön Büro** fact: vacant vs occupied (and later expected arrival). Room Operations must not invent a guest-in-house record.

The readiness model is **capable** of coexisting with occupancy later (stayover: Occupied + Dirty). Sprint 0.9B remains checkout/vacated-centric and does **not** implement Occupancy or Stay merely for stayover support.

**End-of-day discrepancy** (FO occupancy vs Supervisor observed occupancy) is a useful **future** comparison. It is **not** part of the first slice. Do not store “observed occupancy” in 0.9B.

---

## 7. What Room deliberately does not own

- Guest, Reservation, Stay, Folio
- Rates, availability calendar, overbooking
- Employee, Department, Position (Workforce)
- Minibar stock
- `MaintenanceIssue` / technical serviceability (Teknik Servis later)
- Kayıp Eşya
- Passcard / door access
- Public-area cleaning as a Room
- Stored Sellable master status
