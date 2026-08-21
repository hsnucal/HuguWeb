# Kat Hizmetleri operations (participant)

> **Status:** Accepted — Product Owner + CTO approved baseline.
>
> Kat Hizmetleri is a **primary participant** (department and a set of procedures), not the Room Operations module and not the owner of all room operational state.

## 1. What Kat Hizmetleri owns vs consumes

| Consumed (not owned) | Owned in this slice (as work) |
|----------------------|-------------------------------|
| Who is on duty (future Attendance/Shift) | Cleaning/inspection **work items** for rooms |
| Expected arrivals / arrival time (future Reservation) | Assignment of that work to an Employee |
| Guest/stay preparation notes (future Stay) | Priority of **this** work |
| Ready-yesterday list (own readiness, but revalidation is a later work item) | Rework after rejection |
| Passcard/floor access (future Access + Shift) | Completion → Dirty→Clean |
| Daily Employee → Floor roster (future Kat Hizmetleri capability) | Per-room assignment via `EmployeeId` |

Do **not** pull Attendance, payroll, or puantaj into Room Operations because Kat Hizmetleri looks at a roster in the morning.

---

## 2. HousekeepingWorkItem

**Product:** Oda temizlik / hazırlık işi.  
**Technical:** `HousekeepingWorkItem`.

A work item is the **job** to move a room through cleaning (and to receive rework). It is not a generic BPM / workflow engine.

**RoomReadiness ≠ HousekeepingWorkItem.** Example: the room is Dirty (state); “Clean Room 214” is work. Sprint 0.9B should eventually implement both.

### Lifecycle (conceptual)

`Open` → `Assigned` → `Completed` (attendant marked clean) → `Closed` by inspection accept  
or `Open`/`Assigned` again after rejection (`Rework`).

Ended/stale items must not apply their completion to a room that has since been dirtied again (e.g. new checkout).

### What it references

- `RoomId`
- `AssignedEmployeeId` (Workforce `Employee`, optional until assigned)
- `Priority` (domain data)
- reason for rework (from inspection)

For the first slice, `HousekeepingWorkItem` → `EmployeeId` is sufficient. Do not build a floor roster.

### What it does not own

- Employee master data
- Permission to act (Identity)
- Floor as an org unit
- Public-area, beach, trash, and Lost & Found jobs (see below)

---

## 3. Assignment and floors

Typical pattern (PO workflow):

- Each floor generally has a Kat Görevlisi.
- A **Joker** may cover another floor.
- Checkout rooms generally have priority.

Workforce already models **Temporary Assignment** as joker/coverage ([HR-DOMAIN-001](../hr/HR-DOMAIN-001-Organization-Workforce-Foundation.md)). Room Operations **assigns work to Employee id**, it does not re-implement joker employment.

Daily Employee → Floor roster is a **valid future Kat Hizmetleri capability**. It is **out of Sprint 0.9B**.

**0.9B** starts with per-room work assignment (`HousekeepingWorkItem` → `EmployeeId`) without a FloorPlan aggregate.

Kat Görevlisi **must not automatically gain authority** to override business-critical priority. Position ≠ permission. Kat Hizmetleri yönetimi can change priorities. When several urgent rooms conflict, **Ön Büro** may set **business** priority.

---

## 4. Priority

Priority is **domain data** on housekeeping work, not only a UI sort order. The first slice should remain small.

Conceptual first-level values may be:

- `Normal`
- `High`
- `Urgent`

Exact implementation may improve naming if justified. Do **not** create a scoring engine.

| Source | Example |
|--------|---------|
| Default rule | Checkout rooms ahead of stayover/vacant (reference + PO) |
| Consumed fact | Arrival time, later room type |
| HK management | Explicit bump |
| Ön Büro | Conflict among urgent arrivals |
| Rejection | Small correction may become high priority |

Priority changes should later be **auditable** (who, when, from, to, why if given).

---

## 5. Workforce integration

Reuse existing Workforce identity. Do **not** duplicate:

`Employee`, `Department`, `Position`, `Employment`, `Assignment`

Housekeeping work may reference **`EmployeeId`**.

| Concept | Role |
|---------|------|
| Domain actor “attendant who cleaned” | The Employee recorded on the work item / transition |
| Position “Kat Görevlisi” | Org data; may describe the job; **never** checked in code |
| Permission `…clean.complete` | Identity; granted to users who may complete cleaning |
| Joker | Another Employee assigned the work; later Temporary Assignment explains *why* they work that floor |

Do **not** modify Workforce in Sprint 0.9A. Do **not** hard-code Kat Görevlisi / Supervisor / Minibar Görevlisi / Order Taker as authorization roles. They remain customer organizational concepts.

Supervisor as a **manager hierarchy** is deferred in Workforce (`ReportsToEmployeeId` not implemented). Inspection permission is **authorization**, not org-chart.

---

## 6. Extra housekeeping work

These exist in real hotels and are often WhatsApp-coordinated today:

- extra towels/sheets, extra bed, baby bed, pillow
- Kayıp Eşya
- common-area / toilet / trash / sunbed
- guest-request tasks

**Do not** fold all of them into Room Readiness.

**Direction:** a later, **narrow** operational task type for guest-request and extra-room prep **linked to a Room or location**, not an enterprise workflow engine.

Preparation notes from a future reservation (baby bed, honeymoon, accessibility) **create or inform work**; Room Operations **consumes** a preparation-requirement view. It does **not** own Reservation.

**0.9B:** only cleaning/inspection work tied to Dirty→Ready. Extra tasks out.

---

## 7. Morning operations (boundary reminder)

Determining staff on duty, baby-bed prep lists, and “has the guest already checked in?” are **consumed information**. Ownership stays with Attendance/Shift, Stay, and Ön Büro respectively.
