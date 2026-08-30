# HR-06 — Shift & Work Schedule / Vardiya & Çalışma Planı

> **Status:** Accepted / Completed — Product Owner + CTO domain freeze (2026-08-29); Product Owner implementation acceptance (2026-08-30).
>
> Authorizes implementation **only after** [HR-06A-Shift-Schedule-Implementation-Plan.md](HR-06A-Shift-Schedule-Implementation-Plan.md) is followed for **HR-06A**. Domain freeze remains Accepted; HR-06A + HR-06B + AUTH-02 are **Accepted / Completed**.
>
> **Does not supersede** HR-DOMAIN-001, HR-DOMAIN-002, HR-DOMAIN-003, HR-04, or HR-05A. Those remain **Accepted**.
>
> **WebİK** remains a capability reference only. It is not HuGu domain truth, architecture, naming, schema, or UI to copy. Snapshot files are **not** in this repository.
>
> HuGu current domain implementation is the **source of truth** for what already exists.

---

## Slice identity

| | |
|--|--|
| Slice family | **HR-06** Shift & Work Schedule |
| **HR-06A** | Shift & Schedule Foundation (domain + API + definition admin) |
| **HR-06B** | Shift Planning UX / Bulk Planning (Vardiya Planı week grid) |

Do **not** rename because older Personel Master planning used **HR-06–08** for leave / shift / puantaj. Planning map: [README.md](README.md).

---

## 1. Goal

For Employment X on Property-local date D, answer exactly one of:

| State | Meaning |
|-------|---------|
| **Scheduled** | Planned to work (Shift entry + ShiftDefinition) |
| **RestDay** | Intentionally planned not to work |
| **Unscheduled** | No authoritative plan exists yet |

These are **three distinct** operational facts. Never infer RestDay from missing data.

Later consumers (leave charged-day suggestion, attendance, overtime, mobile, staffing) **read** this capability. HR-06 does **not** implement those modules.

---

## 2. Accepted high-level direction

### Adopt from WebİK (capability, not architecture)

- Reusable shift definitions  
- Daily shift assignment  
- Explicit intentional off (OFF → HuGu `RestDay`)  
- Empty ≠ off  
- Overnight support  
- Bulk / multi-cell assignment + previous-week copy (HR-06B)  
- Planned vs actual separation  

### Reject / do not copy

| Reject | Reason |
|--------|--------|
| Leave as schedule-cell truth | Violates HR-05A; `LeaveRecord` independent |
| Empty cell = RestDay | Ambiguous; loses Unscheduled |
| Hardcoded weekend RestDay | Hotels are 24/7 |
| Attendance fields in schedule domain | HR-07 |
| Recurrence / pattern as source of truth | Hotel volatility; daily rows SoT |
| Schedule approval workflow | PO/CTO: direct manage |
| Organization-global ShiftDefinition | Property-local catalogues |

---

## 3. HR-06A / HR-06B split (frozen)

| Slice | Owns |
|-------|------|
| **HR-06A** | `ShiftDefinition`, `ScheduleEntry`, RestDay/Unscheduled semantics, local time + overnight, Assignment-at-date resolution, `GetScheduleState`, permissions, definition admin UI, single-entry APIs, tests |
| **HR-06B** | Vardiya Planı week grid, department filter, multi-select, bulk assign/rest, overwrite confirm, copy previous week |

HR-06A does **not** require the operational week grid.  
HR-06B does **not** reopen domain ownership.

---

## 4. Core domain model (frozen)

```text
Property
  └── ShiftDefinition [0..*]

Employment
  └── ScheduleEntry [0..*]
        └── AssignmentId → Assignment (Primary covering ScheduleDate)
              └── Department → PropertyId
```

`LeaveRecord` is **not** in this hierarchy. HR-05A unchanged.

Department and Position **do not own** schedules. They are filter / display / bulk-selection context only. Do **not** persist `DepartmentId` or `PositionId` on `ScheduleEntry`.

---

## 5. ShiftDefinition (Property-scoped)

### Ownership

**Property-scoped.** Shifts are wall-clock plans in `Property.TimeZoneId`. Hotels may use different catalogues. Do **not** create Organization-global definitions to deduplicate.

### Fields (minimal, frozen)

| Field | Notes |
|-------|--------|
| `Id` | Guid |
| `PropertyId` | Owner |
| `Code` | Property-unique; normalized; **immutable**; reserved after deactivate |
| `Name` | Editable display data |
| `StartLocalTime` | `TimeOnly` — local wall clock |
| `EndLocalTime` | `TimeOnly` — local wall clock |
| `EndsNextDay` | `bool` — **explicit**; never infer solely from End ≤ Start |
| `BreakMinutes` | `int` — single total planned unpaid break |
| `IsActive` | Deactivate; no hard-delete if referenced |
| `CreatedAtUtc`, `CreatedByUserId` | Audit |
| `UpdatedAtUtc`, `UpdatedByUserId` | Audit |

### Forbidden on ShiftDefinition (HR-06)

Payroll/SGK codes, OT coefficients, attendance tolerances, color, pay rates, legal categories, automatic OT rules, multi timed break intervals.

### Code rules

Same spirit as `LeaveType.Code`: normalize (trim, case-insensitive uniqueness), immutable after create, **no reuse** of deactivated codes within the same Property.

### Deactivation

- Cannot newly assign inactive definitions  
- Historical `ScheduleEntry` rows remain readable  
- No hard-delete while referenced  

---

## 6. Break model (frozen)

**`BreakMinutes`** = one total planned break duration for HR-06A.

WebİK multi-`araDinlenmeler` is reference only. Exact break windows may expand in **HR-07** if attendance needs them; planned duration today uses total minutes only.

Compatibility: future interval breaks would refine duration calculation without changing ScheduleEntry Kind model.

---

## 7. Local-time model (frozen)

| Concept | Representation |
|---------|----------------|
| **PLAN** | `DateOnly` + local `TimeOnly` + Property `TimeZoneId` |
| **ACTUAL** (future HR-07) | UTC / `DateTimeOffset` instants |

Do **not** persist ShiftDefinition times as UTC. Do **not** precompute/store UTC planned timestamps on ScheduleEntry.

### DST / IANA

Property uses IANA `TimeZoneId`. HR-06 stores **local schedule intent**. Future attendance maps local planned interval through that timezone for the schedule date(s).

Document for implementers:

- Prefer not inventing nonexistent local times (DST spring-forward gaps) in definitions if ever exposed outside TR; MVP TR hotels typically use `Europe/Istanbul` (no DST since 2016).  
- Overnight spanning a DST transition is a future attendance concern, not a reason to store UTC plans in HR-06.

Do not over-engineer DST UX in HR-06A.

---

## 8. Overnight model (frozen)

Explicit `EndsNextDay`.

| Example | Start | End | EndsNextDay |
|---------|-------|-----|-------------|
| Day | 08:00 | 16:00 | `false` |
| Evening to midnight | 16:00 | 00:00 | `true` |
| Night | 23:00 | 07:00 | `true` |

**ScheduleDate** = Property-local calendar date on which the shift **starts**.

Example: ScheduleDate `2026-08-29`, 23:00→07:00, EndsNextDay=true → planned interval  
`2026-08-29 23:00` → `2026-08-30 07:00` (local).

### Midnight semantics

- `EndLocalTime = 00:00` with `EndsNextDay = true` means end at **midnight at the start of the next local calendar day** (hotel FO 16:00–00:00).  
- `EndLocalTime = 00:00` with `EndsNextDay = false` is **invalid** (zero/ambiguous same-day end).  
- `StartLocalTime == EndLocalTime` is **invalid** in MVP (including with EndsNextDay — would imply 24h; **not** supported). Mark 24h shifts deferred / NEEDS HOTEL VALIDATION later.

---

## 9. Shift duration (derived)

```text
GrossMinutes =
  EndsNextDay
    ? (minutes from Start to midnight) + (minutes from midnight to End)
    : (End - Start)

PlannedNetMinutes = GrossMinutes - BreakMinutes
```

Do **not** persist `PlannedHours` / `PlannedNetMinutes` unless a later Accepted need appears.

### Invariants

- `BreakMinutes >= 0`  
- `BreakMinutes < GrossMinutes`  
- `GrossMinutes > 0`  
- Reject Start==End (MVP)  
- Reject End ≤ Start when `EndsNextDay = false`  
- When `EndsNextDay = true`, End may be ≤ Start in clock order (expected for overnight)

API may reject inconsistent `EndsNextDay` vs clock order (e.g. EndsNextDay=true but End > Start is allowed for evening-to-afternoon? Prefer: if EndsNextDay and End > Start, still valid as “spans next day but ends afternoon” — rare; **validate GrossMinutes > 0 and BreakMinutes &lt; GrossMinutes** as the hard gate; require EndsNextDay=true whenever End ≤ Start).

**Frozen validation rule:**

1. If `EndLocalTime <= StartLocalTime` ⇒ `EndsNextDay` **must** be `true` (except invalid Start==End).  
2. If `EndsNextDay = false` ⇒ `EndLocalTime > StartLocalTime`.  
3. `EndsNextDay = true` with `EndLocalTime > StartLocalTime` is allowed (span into next afternoon).

---

## 10. ScheduleEntry (frozen)

Materialized **daily** row = **source of truth**. No RRULE. No rotating pattern engine in 06A/06B.

### Fields

| Field | Notes |
|-------|--------|
| `Id` | Guid |
| `EmploymentId` | Owner lifecycle |
| `AssignmentId` | **Frozen snapshot/reference** — see §11 |
| `ScheduleDate` | Property-local operating date (shift start date / rest date) |
| `Kind` | `Shift` \| `RestDay` only |
| `ShiftDefinitionId` | Required iff Kind=Shift; null iff RestDay |
| `Note` | Optional short note |
| `CreatedAtUtc`, `CreatedByUserId` | Audit |
| `UpdatedAtUtc`, `UpdatedByUserId` | Audit on change |

### Kind rules

| Kind | ShiftDefinitionId |
|------|-------------------|
| `Shift` | Required; active on **new** assign |
| `RestDay` | Must be null |

No `Leave`, `PublicHoliday`, or `Unscheduled` kind. Unscheduled = **no row**.

### Uniqueness

At most one authoritative `ScheduleEntry` per `(EmploymentId, ScheduleDate)`.

MVP does **not** support split shifts / multiple intervals per day. Deferred + NEEDS HOTEL VALIDATION later.

---

## 11. AssignmentId decision (frozen)

### Repository evidence

| Question | Finding |
|----------|---------|
| A. Historical workplace context? | **Yes.** Transfer **closes** current Primary (`TryCloseOn`) and **creates** a new Assignment row. Rows are retained. |
| B. Destructive rewrite? | **No.** `DepartmentId` / `PositionId` have no mutators after create. Only period end is closed. |
| C. Enforce Employment + Primary + Covers(ScheduleDate)? | **Yes** — application/domain rule on every write (and copy-week target). |
| D. Property resolve? | `AssignmentId` → `Department` → `PropertyId`. `Department.PropertyId` has **no** move API today (immutable after create). |

### Decision

**Store `AssignmentId` on `ScheduleEntry`.**

- Avoids duplicating PropertyId / DepartmentId / PositionId  
- Pins historical workplace context to the Assignment that covered the schedule date at write time  
- Property still derived; **do not** also persist PropertyId  

### Write-time rules

1. Resolve Primary via `PrimaryAssignments.Covering` (reuse; wrap as reusable `ResolvePrimaryAssignmentOnDate` / workplace helper).  
2. If none covers `ScheduleDate` → **fail** with stable error (no first/current/last/default property fallback).  
3. Persist that Assignment’s `Id` as `AssignmentId`.  
4. For Kind=Shift: `ShiftDefinition.PropertyId` must equal Assignment→Department→PropertyId.  

### Copy previous week (06B)

Re-resolve Assignment for **each target date**. Do **not** copy source `AssignmentId` blindly.

### Caveat (document, do not block)

If a future feature ever moves a Department across Properties, Assignment→Property historical meaning would change. That change is out of scope; if introduced later, revisit Property snapshot. **Today PropertyId on Department is immutable → AssignmentId is safe.**

---

## 12. Effective Assignment on date (frozen)

Reusable capability (extend existing concept; do not invent parallel calendars):

```text
ResolvePrimaryAssignmentOnDate(EmploymentId, DateOnly date)
  → Assignment  OR  stable error

ResolveWorkplaceOnDate(...)
  → PropertyId (+ DepartmentId/PositionId as needed)
```

Uses `PrimaryAssignments.Covering`. **No silent fallbacks.**

Scheduling writes **require** a covering Primary Assignment.

---

## 13. Employment period (frozen)

`ScheduleDate` must lie in Employment period:

- `ScheduleDate >= Employment.StartDate`  
- if `EndDate` set: `ScheduleDate <= Employment.EndDate`  

### Overnight past Employment.EndDate (PO/CTO)

**Allowed:** ScheduleDate = Employment.EndDate with EndsNextDay shift ending next local calendar day.

Example: EndDate 30 Aug; ScheduleDate 30 Aug; 23:00→07:00 EndsNextDay → **valid**.

`Employment.EndDate` is a **date** boundary for ScheduleDate, not an exact end timestamp.

Future HR/legal/payroll interpretation may need validation — **does not block HR-06A**.

---

## 14. RestDay / Unscheduled (frozen)

| Fact | Representation |
|------|----------------|
| RestDay | `ScheduleEntry` Kind=`RestDay` |
| Unscheduled | **No** `ScheduleEntry` for that Employment+date |
| Leave | `LeaveRecord` (independent) |
| PublicHoliday | Future calendar domain — not HR-06 |

Do **not** create ShiftDefinition code `OFF`. RestDay is Kind, not a fake shift.

Do not call Unscheduled “Off”, “Rest”, or “Holiday” in domain language.

---

## 15. Clear → Unscheduled (frozen)

Deliberate operation: **ClearScheduleEntry** / unassign.

Removes the authoritative row for that Employment+date so state becomes Unscheduled.

- Auditable (history records before → Unscheduled)  
- Not the same as marking RestDay  
- Prefer explicit API (see §21) over silent hard-delete without history  

---

## 16. Schedule history (frozen)

`ScheduleEntry` is **mutable** (hotels change plans often). No event sourcing.

**Do not** overload `PersonnelProfileChange` (employee profile field diffs).

**Narrow append-only table:** `ScheduleEntryChange` (name finalizable in implementation):

| Field | Purpose |
|-------|---------|
| Id | Guid |
| EmploymentId | Scope |
| ScheduleDate | Day addressed |
| ScheduleEntryId | Nullable after clear |
| PreviousKind | null if Unscheduled→* |
| PreviousShiftDefinitionId | |
| NewKind | null if *→Unscheduled |
| NewShiftDefinitionId | |
| ChangedAtUtc | |
| ChangedByUserId | |

Emit on: Unscheduled→Shift/RestDay, Shift↔Shift, Shift↔RestDay, Shift/RestDay→Unscheduled.

Compact before/after Kind + ShiftDefinitionId is enough; no full JSON blob required.

Also keep `UpdatedAt/By` on the live row when it still exists.

---

## 17. Materialized daily SoT / no weekly pattern (frozen)

Daily `ScheduleEntry` is authoritative.

Copy week / bulk assign **materialize** rows. No live link to source week. No WorkPattern SoT. Optional future template **generator** may create rows later — deferred.

---

## 18. GetScheduleState (frozen contract)

```text
GetScheduleState(EmploymentId, DateOnly date) →

  Unscheduled

  RestDay
    ScheduleEntryId
    Assignment / workplace context

  Scheduled
    ScheduleEntryId
    ShiftDefinition (summary)
    local planned start / end (ScheduleDate + times + EndsNextDay)
    planned net duration (derived)
    Assignment / workplace context
```

Does **not** return Leave. Leave queried independently (HR-05A).

Range query for UI: list states for `[from, to]` per employment/employee.

---

## 19. Permissions (frozen — updated HR-06A auth correction)

| Permission | Capability |
|------------|------------|
| `hr.schedule.read` | View employee schedules in authorized Property / Department context |
| `hr.schedule.manage` | Upsert/clear ScheduleEntry (assign Shift / RestDay) |
| `hr.shift-definition.read` | View Property ShiftDefinitions (needed to assign) |
| `hr.shift-definition.manage` | Create/update/activate/deactivate ShiftDefinitions |

No `hr.schedule.approve`. No Pending/Approved/Rejected schedule states.

**Operational ownership vs domain ownership:**

- `ScheduleEntry` remains Employment-owned; Department does **not** own schedule rows.
- Department is **authorization / filter** context via effective Primary Assignment on the **target date**.
- Property-wide HR (Property membership scope) may manage all departments in that Property without per-department grants.
- Department-limited schedulers (future membership Department scope — **schema pending CTO/PO**) may manage only listed departments; multi-department grants are a set, not a single department.
- Historical reads/writes must follow Assignment covering the schedule date (transfer-safe). Range queries authorize **each row** by that row’s Assignment → Department.

**Typical grants (runtime roles, not hardcoded personas):**

| Actor | Permissions | Scope |
|-------|-------------|-------|
| Hotel HR | schedule read/manage + shift-definition read/manage | Property (or Org) |
| Department scheduler | schedule read/manage + shift-definition **read** | Department set within Property |

ShiftDefinition remains Property-owned; it is never Department-scoped.

**WHAT** = permission; **WHERE** = membership / property context. Same pattern as leave.

No pilot/first/default/current-property fallback for historical dates.

---

## 20. Security notes

- ShiftDefinitions constrained to authorized Property  
- Employee schedule access uses existing employee/workplace guard semantics  
- Historical schedule reads use Assignment→workplace for that entry’s date context  
- Cross-property ShiftDefinition assign rejected  

---

## 21. API proposal (HR-06A — not implemented)

Align with `/api/hr/...` (see leave endpoints).

### Shift definitions

```text
GET    /api/hr/shift-definitions?propertyId=
POST   /api/hr/shift-definitions
PATCH  /api/hr/shift-definitions/{id}
```

PATCH: rename, times, EndsNextDay, BreakMinutes, activate/deactivate — not Code.

### Schedule (employee-addressed; resolve current Employment like leave)

```text
GET    /api/hr/employees/{employeeId}/schedule?from=&to=

PUT    /api/hr/employees/{employeeId}/schedule/{date}
       body: { "kind": "Shift", "shiftDefinitionId": "..." }
          | { "kind": "RestDay", "note": "..."? }

POST   /api/hr/employees/{employeeId}/schedule/{date}/clear
```

**Clear** is an explicit POST (like leave cancel) so Unscheduled is intentional and auditable. Avoid raw DELETE-without-history.

`PUT` upserts the single authoritative row for that date (Shift or RestDay).

Optional later: `GET .../schedule/{date}` for single-day state.

### HR-06B bulk (preview)

```text
POST /api/hr/schedule/bulk
  { employeeIds[], dates[], action: AssignShift|MarkRestDay|Clear, shiftDefinitionId? }
```

**Atomic all-or-nothing** for one submitted bulk operation (CTO preference; matches fail-closed validation). Copy-week is a bulk materialize with the same validation per target cell.

---

## 22. UI

### HR-06A

Small settings surface: **Workforce → Vardiya Tanımları** (or under structure settings). Fields: Code, Name, Start, End, Next-day, BreakMinutes, Active. Consistent with existing HuGu settings UX. **No** week grid.

### HR-06B

**Vardiya Planı:** Property context, week navigation, department filter, employee rows × local date columns, cells Shift/RestDay/Unscheduled, multi-select, assign / RestDay / clear, overwrite confirm, copy previous week.

### Personnel Card

**No** schedule tab/summary in HR-06A (PO/CTO). Primary UX is cross-employee Vardiya Planı. Card views later.

### Leave overlay on grid

Prefer **defer** (not 06B required). If added later, read `LeaveRecord` for display only — never write leave into schedule.

---

## 23. Leave boundary (frozen)

HR-05A unchanged.

Future charged-day **suggestion** may use ScheduleState:

| State | Suggestion hint |
|-------|-----------------|
| Scheduled | potentially chargeable |
| RestDay | potentially not chargeable |
| Unscheduled | cannot safely decide |

Do **not** mutate `LeaveRecord.Amount` in HR-06A/06B.

---

## 24. Public holiday / attendance boundaries

| Domain | Fact type |
|--------|-----------|
| RestDay | Employee schedule |
| PublicHoliday | Calendar/legal — **not** in HR-06A |
| Leave | Absence |
| Shift | Planned work |

HR-06 = **PLAN**. HR-07 = **ACTUAL** (clock, OT, lateness, devices). Planned local interval must be sufficient for later comparison.

---

## 25. Concurrency (frozen recommendation)

Workforce EF model today has **no** RowVersion on HR entities (unlike Technical Service / Room Ops).

| Slice | Approach |
|-------|----------|
| **HR-06A** | Last-write-wins; `UpdatedAtUtc` for diagnostics. No distributed locks. |
| **HR-06B** | Document collaborative overwrite risk; overwrite confirmation in UI; if collisions become real, add optimistic token later (follow Technical Service pattern) — **not** required to start 06A |

---

## 26. Hotel scenario validation (frozen expectations)

| # | Scenario | Expected |
|---|----------|----------|
| A | HK 08:00–16:00 | Def EndsNextDay=false; ScheduleEntry Shift |
| B | FO 16:00–00:00 | EndsNextDay=true; End=00:00 |
| C | Night 23:00–07:00 | EndsNextDay=true; ScheduleDate=start night |
| D | Explicit RestDay | Kind=RestDay; no ShiftDefinition |
| E | No schedule yet | Unscheduled (no row) |
| F | Transfer A→B | Writes on dates after transfer use new Primary/Property; old entries keep prior AssignmentId |
| G | Change after planning | PUT updates row + ScheduleEntryChange |
| H | Leave on Scheduled day | Coexist; Leave independent |
| I | Leave covering RestDay | Coexist; schedule still RestDay |
| J | Employment end mid-month | ScheduleDate ≤ EndDate; overnight start on EndDate allowed |
| K | Irregular part-time | Materialized days; PartTimeMonthlyHours stays contract metadata |
| L | Bulk HK department | Filter by Assignment/dept; bulk all-or-nothing |
| M | Copy week across transfer | Target dates re-resolve Assignment; Property match per target |
| N | Inactive definition + history | Readable; new assign rejected |

---

## 27. Domain invariants (frozen for implementation)

1. One authoritative ScheduleEntry per Employment + ScheduleDate.  
2. Kind=Shift ⇒ ShiftDefinitionId required; Kind=RestDay ⇒ null.  
3. No Leave/PublicHoliday/Unscheduled kinds.  
4. Unscheduled = absence of row.  
5. ScheduleDate within Employment period; overnight end after EndDate allowed if ScheduleDate ≤ EndDate.  
6. Write requires Primary Assignment covering ScheduleDate; store that AssignmentId.  
7. ShiftDefinition.PropertyId must match Assignment→Department→PropertyId.  
8. Inactive definition: no new assign; history readable; Code reserved.  
9. No silent property/assignment fallbacks.  
10. LeaveRecord never stored in ScheduleEntry.  
11. Duration derived; BreakMinutes valid against gross duration.  
12. EndsNextDay explicit per §8–§9.  

---

## 28. Deferred

Attendance/clock/OT/devices; public holiday engine; leave Amount auto-mutation; hourly leave; split shifts; shift swap; employee self-service; schedule approval; recurrence/rotation SoT; schedule templates (optional later generator); payroll; staffing forecast; fairness analytics; 24h shift; multi timed breaks; Personnel Card schedule chrome; leave overlay on grid (prefer defer).

---

## 29. Open questions retained (non-blocking)

### NEEDS HOTEL VALIDATION

- Split shifts / multiple intervals per day  
- Whether early rollout tolerates many Unscheduled days before full RestDay planning  

### NEEDS HR/LEGAL (later)

- Payroll/SGK interpretation of overnight ending after Employment.EndDate  
- RestDay vs paid weekly rest / leave deduction rules  

### NEEDS TECHNICAL (06B)

- Whether optimistic concurrency token becomes necessary after real multi-editor use  

---

## 30. Documentation / WebİK

Discovery evidence summarized in prior Proposed revision remains historically useful; **Accepted** decisions above supersede open recommendations.

WebİK snapshot path (reference only): `C:\Users\hsnuc\Desktop\ik.webik.com.tr` — not in git.

---

## Document control

| | |
|--|--|
| Status | **Accepted** |
| HR-06A plan | [HR-06A-Shift-Schedule-Implementation-Plan.md](HR-06A-Shift-Schedule-Implementation-Plan.md) |
| Related | HR-05A Leave (Accepted); future HR-07 Attendance |
| Freeze date | 2026-08-29 |
