# HR-06B — Weekly Shift Planning

> **Status:** Accepted / Completed — Product Owner acceptance (2026-08-30)  
> **Scope:** Operational Monday–Sunday shift planning UI + week/bulk/copy APIs  
> Completes HR-06 together with HR-06A + AUTH-02. Does **not** redesign HR-06A or change AUTH-02 semantics.

## Purpose

Primary operational screen for:

- Property-wide HR schedulers
- Department-scoped managers

Question answered:

> Who works which shift on each day of the selected week?

Route: `/app/workforce/shift-plan`  
Nav: Workforce → **Vardiya Planı** / **Shift Plan** / **План смен**  
Permission to open: `hr.schedule.read`  
Permission to mutate: `hr.schedule.manage`

## Week semantics

- Week-first UI: **Monday → Sunday**
- Property-local `DateOnly` (`YYYY-MM-DD`) — no browser UTC calendar drift
- Controls: previous week / this week / next week
- Week start is not customizable in MVP

## Department filtering

- Department filter is required for a usable grid
- Property-wide (`allowedDepartmentIds == null`):
  - Filter list = property departments
  - Option **Tüm departmanlar** / all departments (grouped rows)
- Department-scoped:
  - Only authorized department names returned
  - Auto-select when exactly one authorized department
- Backend remains authoritative; UI never invents unauthorized names

## Cell states

| Presentation | Domain | Notes |
|--------------|--------|-------|
| Shift | `ScheduleEntry.Kind = Shift` | Shows ShiftDefinition **Code**; tooltip has name/times/break/net |
| RestDay | `Kind = RestDay` | UI compact **OFF**; label Dinlenme günü / Rest day — **not** an OFF ShiftDefinition |
| Unscheduled | **no row** | Subtle `—` / Planlanmadı — never OFF / leave / holiday |
| OutOfScope / NotEmployed | presentation only | Disabled muted cells; **not** domain enum values |

No Leave overlay. No PublicHoliday engine. No Saturday/Sunday assumptions beyond weekday labels.

## Transfer mid-week

Primary Assignment on the **target date** is authoritative.

Example: Mon–Wed Housekeeping, Thu–Sun Front Office:

- Housekeeping manager sees/edits Mon–Wed; Thu–Sun are OutOfScope with **no FO schedule leakage**
- Front Office manager sees the inverse
- Property-wide HR sees all seven days

One employee row preferred; cell-level workplace resolution (no permanent duplicate rows).

Employment start/end mid-week → NotEmployed cells (not Unscheduled / RestDay).

## Single-cell editing

Click editable cell → assign active ShiftDefinition / RestDay / clear.

Inactive definitions: historical display allowed; new assignment forbidden.

## Bulk selection

- Explicit **Toplu seçim** mode
- Mode on: click toggles editable cells
- Mode off: click opens edit menu
- Out-of-scope cells cannot be selected
- Bulk bar: assign shift / rest day / clear / clear selection

## Overwrite confirmation

Warn when replacing existing Shift or RestDay.

No warning for Unscheduled → Shift/RestDay.

UI confirmation only — **no** backend approval state.

## Copy previous week

- Source = previous Mon–Sun; target = selected week
- Copies Shift + RestDay only (not Unscheduled)
- **Never** reuses source `AssignmentId`
- Each target date re-resolves Employment + Primary Assignment + department auth
- Inactive ShiftDefinition blocks target copy (copy = new assignment)
- Preview shows copy / overwrite / invalid counts
- Invalid operations **block** atomic apply (no silent skip)

## Transaction semantics

`POST /api/hr/schedule/bulk` and copy apply:

- Single DB transaction
- All-or-nothing
- Each successful mutation writes `ScheduleEntryChange` via existing Upsert/Clear domain path
- Rollback includes history rows

## APIs (HR-06B)

| Method | Route | Auth |
|--------|-------|------|
| GET | `/api/hr/schedule/week?weekStart=&departmentId=` | `hr.schedule.read` |
| POST | `/api/hr/schedule/bulk` | `hr.schedule.manage` |
| POST | `/api/hr/schedule/copy-week/preview` | `hr.schedule.manage` |
| POST | `/api/hr/schedule/copy-week` | `hr.schedule.manage` |

HR-06A per-employee day/range endpoints remain unchanged.

## Architecture invariants (audit)

- No `DepartmentId` / `PropertyId` on `ScheduleEntry`
- Workplace via `AssignmentId` at write time
- Unscheduled = absence of row
- RestDay ≠ Unscheduled
- No Leave / PublicHoliday kinds
- No OFF ShiftDefinition
- No recurrence / RRULE
- No attendance / overtime / payroll / approval
- No Personnel Card schedule editing
- No new migration in HR-06B

## Permissions

| Permission | Behavior |
|------------|----------|
| `hr.schedule.read` | View week grid |
| `hr.schedule.manage` | Assign / rest / clear / bulk / copy within scope |
| `hr.shift-definition.read` | Load active catalogue for new Shift assign; historical cells still render from schedule DTO if definitions read is missing |

Department scope from AUTH-02 `UserMembershipDepartmentScope` + `MembershipDepartmentAccess`.

## Deferred (explicitly not HR-06B)

Attendance, clock-in/out, lateness, overtime, public holidays, shift approval, employee requests/swaps, recurrence templates, auto scheduler, staffing forecasts, fairness, leave overlay, payroll, mobile app, Personnel Card schedule editing.

## Manual PO test plan

Use existing ShiftDefinitions (e.g. SABAH / AKŞAM / GECE) and real personnel — do not seed production defaults.

**A. Property-wide HR**  
Open Vardiya Planı → choose week → Housekeeping → assign SABAH → assign GECE → mark RestDay → clear a cell.

**B. Bulk**  
Enable Toplu seçim → select 5 cells → assign SABAH → verify all 5.

**C. Overwrite**  
Select assigned cells → change to AKŞAM → confirm overwrite.

**D. Copy week**  
Move to next week → Önceki haftayı kopyala → inspect preview → confirm.

**E. Department manager**  
Scope user to Housekeeping → only HK departments → Front Office inaccessible.

**F. Transfer**  
Employee with mid-week department transfer → cell-level access by date.

## Related

- Domain: [HR-06-Shift-Work-Schedule.md](HR-06-Shift-Work-Schedule.md)
- Foundation: [HR-06A-Shift-Schedule-Implementation-Plan.md](HR-06A-Shift-Schedule-Implementation-Plan.md)
- Department scopes: [DEPARTMENT_MEMBERSHIP_SCOPE.md](../../security/authorization/DEPARTMENT_MEMBERSHIP_SCOPE.md)
