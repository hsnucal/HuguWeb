# HR-06A — Shift & Schedule Foundation Implementation Plan

> **Status:** Accepted / Completed — Product Owner acceptance (2026-08-30)
>
> **Auth (AUTH-02, 2026-08-29):** Department narrowing is delivered via `UserMembershipDepartmentScope`
> (child of Property membership; zero rows = property-wide). Schedule endpoints resolve
> `AllowedDepartmentIds` through `MembershipDepartmentAccess`. See
> [DEPARTMENT_MEMBERSHIP_SCOPE.md](../../security/authorization/DEPARTMENT_MEMBERSHIP_SCOPE.md).
> This is **not** `AuthorizationScopeType.Department`. AUTH-02 is **Accepted / Completed**.
>
> **Domain freeze:** [HR-06-Shift-Work-Schedule.md](HR-06-Shift-Work-Schedule.md) is **Accepted**.
>
> Does **not** supersede HR-DOMAIN-001 / 002 / 003, HR-04, or HR-05A.
>
> WebİK remains reference only.
>
> This plan covers **HR-06A**. HR-06B (Vardiya Planı / bulk) is separately Accepted / Completed.

---

## Implementation notes (HR-06A delivered)

| Item | Detail |
|------|--------|
| Migration (Workforce) | `20260829203852_AddShiftScheduleFoundationHr06A` |
| Tables | `ShiftDefinitions`, `ScheduleEntries`, `ScheduleEntryChanges` |
| Module | `HuGuWeb.Workforce` (no new module) |
| AssignmentId | Server resolves Primary via `EffectiveAssignmentResolver` / `PrimaryAssignments.Covering` on every write |
| Semantic lock | Time fields locked after first schedule use; Name + IsActive editable; Code immutable |
| Permissions | `hr.schedule.read/manage`, `hr.shift-definition.read/manage` |
| Schedule access | `ScheduleAccess.AllowsWorkplace` — null department set = Property-wide |
| Frontend | Shift Definitions gated by `hr.shift-definition.*` — **no** week grid |

### Department scope — AUTH-02 delivered

Identity migration: `20260829210313_AddDepartmentMembershipScopes`.

1. Keep `AuthorizationScopeType` = Organization | Property only (no Department scope type).
2. Child table `UserMembershipDepartmentScope` under Property memberships.
3. Zero rows = Property-wide; one or more rows = allowed department set.
4. Schedule endpoints wire `AllowedDepartmentIds` via `MembershipDepartmentAccess`.
5. Minimal admin UI on Users membership editor (property-wide vs selected departments).

### APIs

```text
GET/POST/PATCH /api/hr/shift-definitions   (shift-definition read/manage)
GET/PUT/POST clear employee schedule…     (schedule read/manage)
```

Range: each row authorized by Assignment→Department on that date (transfer-safe).

### Tests

ShiftDefinition / ScheduleEntry / ScheduleSecurity / ScheduleDepartmentScope + frontend permission helpers.

---

## Constraints carried into every phase

- No attendance, clock, OT, devices, public holiday engine, payroll, approval, recurrence SoT, leave Amount mutation.
- No `Leave` / `PublicHoliday` / `Unscheduled` kinds on ScheduleEntry.
- No Organization-global ShiftDefinition.
- No PropertyId / DepartmentId / PositionId duplicated on ScheduleEntry (use `AssignmentId`).
- No Personnel Card schedule UI in 06A.
- No week-grid / bulk / copy-week in 06A.
- FK `OnDelete(DeleteBehavior.Restrict)` — same as Workforce.
- Routes under `/api/hr/...`.
- Actor ids: string Identity user id, max 450.
- Architecture tests: allow ShiftDefinition / ScheduleEntry / ScheduleEntryChange; keep LeaveBalance / LeaveRequest / ShiftAssignment-as-leave-cell forbidden patterns aligned with HR-05A intent.
- Module: `HuGuWeb.Workforce` (existing). Do **not** create a new module.

---

## Phase 1 — Domain + assignment-at-date resolution

### Enums

```csharp
public enum ScheduleEntryKind
{
    Shift = 1,
    RestDay = 2
}
```

### ShiftDefinition

- Property-scoped create/rename/deactivate/update times.
- Code normalize + immutable; Property+Code unique including inactive.
- Validate local times + `EndsNextDay` + `BreakMinutes` per Accepted §8–§9.
- Derived duration helpers (gross / net minutes) — not persisted.

### ScheduleEntry

- Factory/upsert semantics for Shift vs RestDay.
- Enforce Kind ↔ ShiftDefinitionId rules.
- Store `AssignmentId` resolved at write.
- Clear removes authoritative presence (application orchestrates delete + history).

### ResolvePrimaryAssignmentOnDate

- Reuse `PrimaryAssignments.Covering`.
- Stable application error when missing (no fallbacks).
- Helper to resolve PropertyId via Department.

### ScheduleEntryChange

- Append-only compact history (Accepted §16).

### Tests (domain unit)

Overnight, break, Start==End reject, Kind rules, code immutability, inactive assign reject (application may own some).

---

## Phase 2 — Persistence + constraints + migration design

**Do not invent table names that fight EF conventions; expected:**

### `ShiftDefinitions`

| Column | Notes |
|--------|--------|
| Id | PK |
| PropertyId | FK → Properties, Restrict |
| Code | required, max 32 |
| Name | required, max 200 |
| StartLocalTime | `time` |
| EndLocalTime | `time` |
| EndsNextDay | bool |
| BreakMinutes | int |
| IsActive | bool |
| CreatedAtUtc, CreatedByUserId | |
| UpdatedAtUtc, UpdatedByUserId | |

- Unique index: `(PropertyId, Code)` (normalized uniqueness enforced in domain; DB unique on stored form)
- Index: `(PropertyId, IsActive)`
- Check: `BreakMinutes >= 0` (stronger duration checks in domain)

### `ScheduleEntries`

| Column | Notes |
|--------|--------|
| Id | PK |
| EmploymentId | FK → Employments, Restrict |
| AssignmentId | FK → Assignments, Restrict |
| ScheduleDate | date |
| Kind | int / enum |
| ShiftDefinitionId | FK → ShiftDefinitions, Restrict, nullable |
| Note | optional max length |
| CreatedAtUtc, CreatedByUserId | |
| UpdatedAtUtc, UpdatedByUserId | |

- **Unique:** `(EmploymentId, ScheduleDate)`
- Index: `(AssignmentId)`, `(ShiftDefinitionId)`, `(EmploymentId, ScheduleDate)` (unique covers)
- Check: Kind/ShiftDefinitionId consistency if expressible in SQL; else domain-only

### `ScheduleEntryChanges`

| Column | Notes |
|--------|--------|
| Id | PK |
| EmploymentId | FK Restrict |
| ScheduleDate | date |
| ScheduleEntryId | Guid? (null after clear) |
| PreviousKind | int? |
| PreviousShiftDefinitionId | Guid? |
| NewKind | int? |
| NewShiftDefinitionId | Guid? |
| ChangedAtUtc | |
| ChangedByUserId | |

- Index: `(EmploymentId, ScheduleDate, ChangedAtUtc)`

Migration naming follow existing Workforce style (`AddShiftScheduleFoundationHr06A`).

**Delivered:** `20260829203852_AddShiftScheduleFoundationHr06A` applied to local Workforce DB.

---

## Phase 3 — Application / API + GetScheduleState

### Use cases / queries

- ShiftDefinitionAdmin (list/create/patch) scoped by Property + workplace
- UpsertScheduleEntry (PUT semantics)
- ClearScheduleEntry (→ Unscheduled + history)
- GetScheduleState / GetScheduleRange for employee
- Employment resolution by covering date (not current-open fallback for historical dates)

### GetScheduleState

Return Scheduled / RestDay / Unscheduled DTOs with derived planned interval for Shift.

---

## Phase 4 — Authorization

- Register `hr.schedule.read` / `hr.schedule.manage`
- Seed/grant patterns consistent with `hr.leave.*`
- Endpoint policies: read vs manage
- Workplace/property membership for definitions
- Org membership for employee schedule routes; per-date property scope via effective Assignment (no silent property fallback)

---

## Phase 5 — ShiftDefinition administration frontend

- Workforce nav link when `hr.schedule.read` / manage
- List + create + edit + deactivate for current Property context
- Match existing HuGu settings UX (departments/positions/leave-types patterns)
- **No** Vardiya Planı grid

---

## Phase 6 — Tests

Covered by `ShiftDefinitionTests`, `ScheduleEntryApplicationTests`, `ScheduleSecurityTests`, frontend form/access tests.

---

## Phase 7 — Manual PO acceptance

Checklist:

1. Create Property-scoped shift definitions (day, evening-to-midnight, night).  
2. Assign Shift / RestDay via API (or thin admin + API).  
3. Verify GetScheduleState three states.  
4. Clear → Unscheduled; history shows transition.  
5. Transfer employee; schedule on each side of transfer date resolves correct Property.  
6. Inactive definition blocked for new assign; old entry still returns definition summary.  
7. Overnight on EndDate accepted.  
8. Permissions: read-only cannot manage.  
9. Confirm no leave Amount change; no attendance; no week grid required for 06A sign-off.

Suggested manual definitions (create via UI — not seeded):

| Code | Times | Break | Ends next day |
|------|-------|-------|---------------|
| SABAH | 08:00–16:00 | 30 | no |
| AKSAM | 16:00–00:00 | 30 | yes |
| GECE | 23:00–07:00 | 30 | yes |

---

## HR-06B — Future implementation phases (high level only)

Do **not** implement until HR-06A Accepted by PO.

| Phase | Scope |
|-------|--------|
| B1 | Schedule range query tuned for week grid; department/employee filters |
| B2 | Vardiya Planı UI (week nav, cells, legends) |
| B3 | Multi-select assign Shift / RestDay / Clear + overwrite confirmation |
| B4 | Bulk API all-or-nothing; copy previous week (re-resolve Assignment per target date) |
| B5 | Collaborative UX warnings; revisit optimistic concurrency if needed |
| B6 | PO acceptance on operational planning |

Leave overlay on cells: **deferred** unless PO reopens.

---

## Explicit non-goals (06A)

Week grid, bulk, copy week, approval, attendance, holidays, templates/RRULE, Personnel Card schedule tab, color on definitions, multi-break intervals, PropertyId column on ScheduleEntry.

---

## Document control

| | |
|--|--|
| Status | **Implemented / Awaiting Product Owner + CTO Acceptance** |
| Domain | HR-06 Accepted |
| Slice | HR-06A only |
| Auth schema | Department membership scope **pending CTO/PO** — no Identity migration yet |
