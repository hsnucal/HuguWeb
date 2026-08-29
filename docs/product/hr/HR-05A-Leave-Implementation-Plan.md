# HR-05A — Leave Foundation Implementation Plan

> **Status:** Accepted / Completed — Product Owner manual acceptance (2026-08-29).
>
> **Domain freeze:** [HR-05A-Leave-Foundation.md](HR-05A-Leave-Foundation.md) remains **Accepted**.
>
> Does **not** supersede HR-DOMAIN-001 / 002 / 003 or HR-04.
>
> WebİK remains reference only.
>
> **Closure notes:** PO manual acceptance completed. Leave flows accepted. Final date / payment / Turkish IBAN UX accepted. HR-05A is closed.

This file records the implementation plan and, after coding, the implementation-level deviations below.

---

## Constraints carried into every phase

- No `LeaveRequest`, no approval workflow, no manager model, no shift/attendance, no payroll.
- No `Employee` / `Employment` column for remaining balance.
- No `PropertyId` on leave tables.
- No `WorkCalendar` / holiday engine.
- Amount quantum **0.5 day**; `numeric(6,1)`.
- FK `OnDelete(DeleteBehavior.Restrict)` — same as current Workforce mappings.
- Routes under `/api/hr/...` (not `/api/workforce/employees/...`).
- Actor ids: string Identity user id, max 450.
- Architecture tests: allow `LeaveType` / `LeaveEntitlement` / `LeaveRecord`; keep `LeaveBalance` and `LeaveRequest` forbidden.

---

## Phase 1 — Domain

Module: `HuGuWeb.Workforce` (existing). Do **not** create a new module.

### Enums (PascalCase, like `EmploymentContractType`)

```csharp
public enum LeaveTypeSystemKind
{
    Annual = 1,
    Unpaid = 2,
    Sick = 3,
    Marriage = 4,
    Paternity = 5,
    Maternity = 6,
    Bereavement = 7,
    Excuse = 8,
    Administrative = 9,
    Other = 10
}

public enum LeaveEntitlementSource
{
    Entitlement = 1,
    CarryOver = 2,
    ManualAdjustment = 3
}

public enum LeaveRecordStatus
{
    Recorded = 0,
    Cancelled = 1
}
```

Store as integers in PostgreSQL (same as other Workforce enums).

### Types

`LeaveType`, `LeaveEntitlement`, `LeaveRecord` — sealed classes, private setters, `TryCreate` / domain actions. Factory validation in domain (amount quantum, source sign rules, date order).

**LeaveType actions:** `TryCreate`, `TryRename`, `TrySetTracksBalance`, `Deactivate` (no activate-if-referenced-code-reuse). No `TryChangeCode`. No `TrySetSystemKind` after create.

**LeaveEntitlement:** create only. No mutate.

**LeaveRecord actions:** `TryCreate`, `TryCancel(reason, actor, utc)` — Recorded → Cancelled only.

### Amount helper

Single domain helper: amount is finite, scale ≤ 1 decimal, `amount * 2` is integral (multiple of 0.5), not zero where forbidden.

### Overlap

Pure function: two closed `DateOnly` ranges overlap iff `a.Start <= b.End && b.Start <= a.End`. Apply only among `Status == Recorded` for the same `EmploymentId`.

### Constants

Align with Department where useful: `CodeMaxLength = 32`, `NameMaxLength = 200`. Notes/reason: cap (e.g. 500) like `PersonnelProfileChange.ValueMaxLength`.

### Architecture test

Update `Hr01C_DoesNotIntroduceDeferredHrPlatformTypes` (or a dedicated HR-05A fact):

- **Must contain** `LeaveType`, `LeaveEntitlement`, `LeaveRecord`
- **Must not contain** `LeaveBalance`, `LeaveRequest`
- `Employee` still has no remaining-balance property

Unit tests in `tests/HuGuWeb.UnitTests/Workforce/` for create/cancel/overlap/quantum/period bounds (in-memory entities, no DB).

---

## Phase 2 — Persistence

`HuGuWeb.Workforce.Infrastructure` — `WorkforceDbContext` configurations + **one** new EF migration (generated at implementation time, not in this docs task).

### Tables

**`LeaveTypes`**

| Column | Type | Notes |
|--------|------|--------|
| Id | uuid PK | |
| OrganizationId | uuid NOT NULL | FK Organizations Restrict |
| Code | varchar(32) NOT NULL | |
| Name | varchar(200) NOT NULL | |
| SystemKind | int NULL | |
| TracksBalance | bool NOT NULL | |
| IsActive | bool NOT NULL | |
| CreatedAtUtc | timestamptz NOT NULL | |
| CreatedByUserId | varchar(450) NOT NULL | |
| UpdatedAtUtc | timestamptz NOT NULL | |
| UpdatedByUserId | varchar(450) NOT NULL | |

Unique index: `(OrganizationId, lower(Code))` — use a computed/stored normalized column **or** unique index on `(OrganizationId, Code)` plus domain normalization to lowercase (prefer **normalize Code to lower invariant in domain**, unique on `(OrganizationId, Code)`). HuGu PersonnelNumber already normalizes; follow that pattern.

Indexes: `OrganizationId`, `(OrganizationId, IsActive)`.

**`LeaveEntitlements`**

| Column | Type | Notes |
|--------|------|--------|
| Id | uuid PK | |
| EmploymentId | uuid NOT NULL | FK Employments Restrict |
| LeaveTypeId | uuid NOT NULL | FK LeaveTypes Restrict |
| EffectiveDate | date NOT NULL | |
| Amount | numeric(6,1) NOT NULL | |
| Source | int NOT NULL | |
| Note | varchar(500) NULL | |
| CreatedAtUtc | timestamptz NOT NULL | |
| CreatedByUserId | varchar(450) NOT NULL | |

Check: `Amount <> 0`. Optional check: quantum via `Amount * 2 = trunc(Amount * 2)` if portable; otherwise domain-only.

Indexes: `(EmploymentId, LeaveTypeId, EffectiveDate)`, `LeaveTypeId`.

**`LeaveRecords`**

| Column | Type | Notes |
|--------|------|--------|
| Id | uuid PK | |
| EmploymentId | uuid NOT NULL | FK Employments Restrict |
| LeaveTypeId | uuid NOT NULL | FK LeaveTypes Restrict |
| StartDate | date NOT NULL | |
| EndDate | date NOT NULL | |
| Amount | numeric(6,1) NOT NULL | |
| Status | int NOT NULL | |
| Note | varchar(500) NULL | |
| CreatedAtUtc | timestamptz NOT NULL | |
| CreatedByUserId | varchar(450) NOT NULL | |
| CancelledAtUtc | timestamptz NULL | |
| CancelledByUserId | varchar(450) NULL | |
| CancellationReason | varchar(500) NULL | |

Checks: `StartDate <= EndDate`, `Amount > 0`. Cancelled columns null iff Status = Recorded.

Indexes: `(EmploymentId, Status, StartDate, EndDate)`, `(EmploymentId, LeaveTypeId)`, `LeaveTypeId`.

**No filtered unique** “one recorded leave per day” in SQL (ranges, not a day table). Overlap enforced in application/domain when creating.

### Seed / defaults

**Not** in the migration (no tenant ids at migrate time).

`EnsureDefaultLeaveTypes(organizationId)` in Application + store. Development seeder calls it for the demo org. Production: lazy on first `GET /api/hr/leave-types` (and on POST type if list empty of system codes).

### IWorkforceStore

Add list/get/add methods for the three types; employment-scoped queries for entitlements/records. No generic repository.

### Migration risk checklist (must hold)

- Existing `Employees` / `Employments` / `Assignments` **unchanged**
- No remaining-balance column
- No PropertyId on new tables
- No shift FK
- Restrict delete: cannot delete Employment/LeaveType while children exist (same as Assignments → Employment)

---

## Phase 3 — Application / API

Follow `HrEmployeeEndpoints` + use-case classes in `HuGuWeb.Workforce.Application`.

### Proposed routes

Existing convention is `/api/hr/employees/{id:guid}/...` with antiforgery on writes.

| Method | Path | Policy | Purpose |
|--------|------|--------|---------|
| GET | `/api/hr/leave-types` | HrLeaveRead | Org types (ensure defaults). Query `activeOnly` optional |
| POST | `/api/hr/leave-types` | HrLeaveManage | Custom type (`SystemKind` omitted/null). Code+Name+TracksBalance |
| PATCH | `/api/hr/leave-types/{leaveTypeId:guid}` | HrLeaveManage | Name, TracksBalance, IsActive (deactivate only toward false in MVP; reactivation **allowed** if Code unused by another row — same row only) |
| GET | `/api/hr/employees/{employeeId:guid}/leave` | HrLeaveRead | Summary + entitlements + records for employment |
| POST | `/api/hr/employees/{employeeId:guid}/leave-entitlements` | HrLeaveManage | Create movement |
| POST | `/api/hr/employees/{employeeId:guid}/leave-records` | HrLeaveManage | Create Recorded leave |
| POST | `/api/hr/employees/{employeeId:guid}/leave-records/{recordId:guid}/cancel` | HrLeaveManage | Cancel |

`GET .../leave` query: optional `employmentId`. Default: current non-ended Employment, else latest ended (same idea as tenant covering date). Response must include `employmentId` used.

Do **not** use `/api/workforce/employees/...` for leave.

### GET leave payload (conceptual)

- `employmentId`, employment period/status
- `balances[]`: leaveTypeId, code, name, systemKind, tracksBalance, entitledNet, used, remaining (only tracking types, or include tracking=false with remaining omitted)
- `entitlements[]`
- `records[]`

Balance computed in application from loaded rows (no SQL view required in 05A).

### Guards (every employee-scoped call)

1. `EmployeeTenantGuard.AllowsEmployeeAsync`
2. Employment belongs to that employee
3. LeaveType.OrganizationId == Employee.OrganizationId
4. New writes: type `IsActive`
5. Period + overlap + amount rules
6. Ended/scheduled: period rule only (see freeze)

Validation errors: same `WorkforceResult` / field+code style as HR-04 (`HrValidation`).

Antiforgery on all POST/PATCH. `RequireAuthorization` policies: add `HrLeaveRead` / `HrLeaveManage` in `AuthorizationPolicies` + `SecurityExtensions` (read = read **or** manage claim).

Leave-type POST/PATCH: organization from claims; no cross-org id.

---

## Phase 4 — Authorization

| Artifact | Change |
|----------|--------|
| `HrLeavePermissions` | `Read = "hr.leave.read"`, `Manage = "hr.leave.manage"` |
| `PermissionCatalog.All` | append both |
| `SystemRoleTemplates.HumanResourcesPermissions` | append both |
| `AuthorizationPolicies` | `HrLeaveRead`, `HrLeaveManage` |
| `SecurityExtensions` | policies as above |
| i18n `authorization` | TR/EN/RU labels for the two codes (`types.ts` + locale files) |
| Frontend `hrAccess` | `canReadHrLeave` / `canManageHrLeave` (mirror employee helpers) |

Do not add `hr.leave.approve`.

Dev personas using `PermissionCatalog.All` pick up the new codes automatically.

---

## Phase 5 — Frontend

Personnel Card only. **No** Workforce subnav item. **No** İzin Yönetimi route.

### i18n

`src/frontend/web/src/i18n/hr/{tr,en,ru}.ts` + `types.ts`:

- `tabLeave`: İzinler / Leave / Отпуска
- Section titles, column headers, actions, empty states, amount hint, overlap/period errors
- System kind labels for seed types
- Entitlement source labels

### Card

- Insert tab between payment and history (`PersonnelCard.tsx` tab list).
- Hide when `createMode` or missing `hr.leave.read`.
- Load `GET .../leave` when tab selected (edit mode).
- Empty state: no movements yet; still show seeded type remaining as 0 / 0 / 0 for tracking types if entitlements empty.
- Dialogs: add entitlement (type, date, source, amount, note); add leave (type, start, end, amount with calendar-day **suggestion**, note); cancel (required reason).
- After success: refetch leave; do not mix into employee PUT.

Amount input: step 0.5; client validation matching quantum.

Suggested days: `end - start + 1` as hint only; user can change Amount.

### Types API client

`src/frontend/web/src/workforce/` API module next to existing `getHrEmployee` — do not put leave on the hire payload.

---

## Phase 6 — Tests

### Domain (unit)

- Type: unique code per org (store/integration); inactive cannot be used for new record; historical FK remains
- Entitlement: +grant, +carry, ±adjustment, reject 0, reject negative grant/carry, reject bad type org
- Record: valid range, before StartDate, after EndDate, Amount > 0, quantum, overlap, cancel, double cancel
- Balance: entitled − recorded; cancelled excluded; non-tracking type unused in remaining

### Application / API (unit or integration as existing HR tests)

- Tenant: other org 404/forbid
- Property-scoped HR cannot read employee at another property (`EmployeeTenantGuard`)
- `hr.leave.read` cannot POST; `hr.leave.manage` can
- Missing permission 403
- Ended employment in-period create OK; out-of-period rejected

### Architecture

- Guard updated as Phase 1

### Frontend

- Create mode: no leave tab
- Empty state
- Add entitlement / add leave / cancel refresh summary
- Translation keys present for tr/en/ru (existing i18n completeness tests if any)

Prefer extending `tests/HuGuWeb.UnitTests/Workforce/` and architecture tests; add API tests in the same style as hire/end-employment.

---

## Phase 7 — Manual PO acceptance

Checklist (not automated):

1. Hire a person — İzinler tab absent until save.
2. Open İzinler — default types listed; annual remaining 0.
3. Add grant +14 annual — remaining 14.
4. Add leave 5 days — remaining 9; history Recorded.
5. Cancel with reason — remaining 14; row Cancelled.
6. ManualAdjustment −2 — remaining 12; visible in Hakediş ve düzeltmeler.
7. Overlapping dates rejected.
8. Half-day 0.5 accepted; 0.33 rejected.
9. Custom type created; used in a record; deactivate; old row still visible; new record cannot use it.
10. Property-scoped HR: other property’s employee blocked.
11. User with employee read but **not** leave read: no İzinler tab.
12. TR / EN / RU labels on tab and dialogs.
13. No İzin Yönetimi in sidebar.
14. Confirm no payroll, shift, or approval UI appeared.

---

## Implementation order (strict)

1. Domain + unit tests  
2. EF mapping + migration + store  
3. EnsureDefaults + use cases  
4. Permissions + policies  
5. Endpoints  
6. Frontend tab  
7. Architecture + permission i18n  
8. Manual PO pass  

Do not start frontend before GET leave exists. Do not generate migration before domain fields are frozen in code to match this plan.

---

## Explicitly not in this implementation

- HR-05B request/approval  
- Shift/attendance  
- Payroll  
- Main İzin Yönetimi screen  
- Hourly leave  
- Legal auto-accrual  
- `LeaveBalance` entity  
- `LeaveRequest` entity

---

## Implementation status notes (Accepted / Completed)

**Domain freeze is unchanged (Accepted).** Product Owner manual acceptance completed (2026-08-29). Leave flows, final date/payment/IBAN UX, and Personnel Card leave surfaces were accepted. HR-05A is closed.

Implementation-level deviations from the original plan (unchanged):

1. **Default LeaveTypes are not created by GET.** CTO override: `GET /api/hr/leave-types` and `GET /api/hr/employees/{id}/leave` are read-only. Defaults are ensured idempotently for every organization at API startup (`EnsureDefaultLeaveTypesUseCase.ExecuteForAllOrganizationsAsync` from `Program.cs`), after the development seeder in Development. Missing codes are inserted; existing codes (including deactivated system codes) are not overwritten or revived.

2. **Entitlement movements are rejected when `TracksBalance = false`** (`leave-entitlement-balance-not-supported`). Leave **records** of non-tracking types remain allowed. The Accepted domain freeze allows recording leave of non-tracking types; it does not require entitlement rows for those types.

3. **LeaveType administration** is a small Workforce settings surface at `/app/workforce/leave-types` (same visual language as Resmî ayarlar). There is still **no** standalone İzin Yönetimi module in primary navigation.

4. **Migration:** `20260829124940_AddLeaveFoundationHr05A` — adds only `LeaveTypes`, `LeaveEntitlements`, `LeaveRecords`.

5. **Final PO UX (accepted):** İzin Ekle Start/End use the shared `DateField` calendar picker. İzin Türü lists the active Annual `SystemKind` first (not localized Name). End Date uses `minDate = StartDate` so the calendar cannot pick an earlier day; same-day remains allowed. If Start moves past End, End snaps to the new Start. Manual End-before-Start still uses the existing `invalidDateRange` path. Suggested Amount stays inclusive calendar days; HR confirms the authoritative Amount. Payment tab keeps Banka adı before IBAN; Turkish IBAN uses fixed TR prefix, digits-only body, display grouping, and canonical no-space persistence.

6. **Deferred — Shift/Attendance charged days:** HR-05A does **not** exclude weekends, public holidays, or weekly-off days from Amount. HuGu does not yet know the employee’s actual day-off / shift schedule, and hotels operate 24/7, so Mon–Fri or Saturday/Sunday-off assumptions are invalid. A future Shift/Attendance integration may calculate charged leave from scheduled working days / weekly-off days. That remains out of Accepted domain scope.
 
