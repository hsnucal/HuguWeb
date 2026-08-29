# HR-05A — Leave Foundation / İzin Yönetimi Temeli

> **Status:** Accepted — Product Owner + CTO domain freeze (2026-08-29).
>
> Authorizes implementation **only after** [HR-05A-Leave-Implementation-Plan.md](HR-05A-Leave-Implementation-Plan.md) is reviewed. This document does **not** itself ship code.
>
> **Does not supersede** HR-DOMAIN-001, HR-DOMAIN-002, HR-DOMAIN-003, or HR-04. Those remain **Accepted**.
>
> **WebİK** remains a capability reference only. It is not HuGu domain truth, architecture, naming, or a schema to copy.
>
> HuGu current domain implementation is the **source of truth**.

---

## Slice identity

| | |
|--|--|
| Slice id | **HR-05A** |
| EN | Leave Foundation |
| TR | İzin Yönetimi Temeli |

Do **not** rename this feature because older Personel Master planning used **HR-05** for temporary assignment / promotion, and **HR-06–08** for leave / shift / puantaj. Those Accepted texts are **not rewritten**. Planning map: [README.md](README.md).

**HR-05A** = types, entitlement movements, HR-entered records, derived balance, Personnel Card **İzinler**, permissions, tenant isolation.

**HR-05B** = request / approval / notifications / mobile / HR-wide list. Not this slice.

---

## Accepted product direction

WebİK has leave. HuGu will **not** copy these architectural choices:

| WebİK (confirmed) | HuGu |
|-------------------|------|
| No standalone İK “İzin Yönetimi” menu | Same for 05A (card only). List page later. |
| Usage = TimeCore shift cells | Independent `LeaveRecord`. No shift tables. |
| Remaining = formula + mutable `yiDevir` on personel | Derived only. No stored remaining. |
| Configurable types matched by **name** `"Yıllık İzin"` | `SystemKind` + org-unique `Code`. Never match on localized name. |
| HR can enter leave | **Adopt** as `LeaveRecord` with `Origin` reserved for 05B |
| Approval in some flows | **Defer** HR-05B |
| Reports / calendar | **Defer** HR-05B / follow-up |
| Hourly leave, separate store | **Defer** Shift/Attendance or 05B |
| Range paints every calendar day | Dates stored; **Amount** is authoritative |
| Weak audit (overwrite cell) | Cancel-retain; entitlement movements immutable |

---

## Frozen ownership

```text
Organization
  └── LeaveType [0..*]

Employee                         (identity only — no leave columns)
  └── Employment [1..*]
        ├── LeaveEntitlement [0..*]
        └── LeaveRecord      [0..*]
```

Leave is **not** on `Employee`. Entitlement and usage belong to an **Employment** lifecycle. Rehire = new Employment, new movements; prior rows stay on the ended Employment.

No `PropertyId` on leave rows. Workplace visibility uses existing `EmployeeTenantGuard` (current/last Primary Assignment → Department.PropertyId). No default/first-property fallback.

`EmploymentStatus` stays `Scheduled | Active | Ended`. A person on recorded leave remains **Active**. Daily availability is not Employment (INVARIANTS §10).

Seniority: reuse `Employment.EffectiveSeniorityDate` (`SeniorityStartDate ?? StartDate`). **No** `LeaveSeniorityDate`. 05A does **not** auto-grant from seniority.

---

## LeaveType

System defaults **plus** organization customization. **Not** a closed enum of types. Hotels add custom categories without a deployment.

Stable semantics **must not** depend on `Name`.

Forbidden: `if (leaveType.Name == "Yıllık İzin")`.

### Fields

| Field | Notes |
|-------|--------|
| `Id` | Guid |
| `OrganizationId` | Owner. No global shared type table. |
| `Code` | Org-unique (case-insensitive). Immutable after create. Max 32, slug-like. |
| `Name` | Organization working name (data). Editable. Not a semantic key. |
| `SystemKind` | `LeaveTypeSystemKind?`. Null = custom. |
| `TracksBalance` | Explicit. **Not** derived from `SystemKind` alone. |
| `IsActive` | Deactivate; never hard-delete if referenced. |
| `CreatedAtUtc`, `CreatedByUserId` | Audit |
| `UpdatedAtUtc`, `UpdatedByUserId` | Rename / TracksBalance / deactivate |

Do **not** add in 05A: `IsPaid`, `RequiresDocument`, SGK/payroll/shift codes, colour, overtime, attendance flags.

`Code` uniqueness: **`OrganizationId` + normalized `Code` unique**, including inactive rows. **No reuse** of a deactivated code in the same organization.

Inactive type: remains on historical entitlements/records; cannot be selected for **new** entitlements or records.

### `LeaveTypeSystemKind` (closed domain enum)

`Annual`, `Unpaid`, `Sick`, `Marriage`, `Paternity`, `Maternity`, `Bereavement`, `Excuse`, `Administrative`, `Other`.

Custom types: `SystemKind = null`. Do not force a kind on hotel-created types.

UI labels for seeded kinds: i18n `tr` / `en` / `ru` keyed by `SystemKind`. Working `Name` is fallback/export and org rename.

### `TracksBalance`

When `true`, this type participates in entitlement/usage/remaining.

Seed: **Annual = true**; all other seeded types **false**. Hotels may change `TracksBalance` (including on custom types). Remaining is only shown for types with `TracksBalance = true`.

Recording leave of a non-tracking type is allowed (history/ops) and **does not** affect any remaining calculation.

---

## LeaveEntitlement (movement, not a snapshot)

Auditable fact. **Not** current remaining.

| Field | Notes |
|-------|--------|
| `Id` | Guid |
| `EmploymentId` | Owner |
| `LeaveTypeId` | Must belong to the same Organization as the employee |
| `EffectiveDate` | `DateOnly` — when the movement applies |
| `Amount` | `decimal`, signed, **≠ 0**, half-day quantum (see Amount) |
| `Source` | `LeaveEntitlementSource` |
| `Note` | Required for `ManualAdjustment`; optional otherwise |
| `CreatedAtUtc`, `CreatedByUserId` | |

**Immutable after create.** No silent edit. Correction = another movement.

### `LeaveEntitlementSource` (closed enum)

| Value | Meaning |
|-------|---------|
| `Entitlement` | Normal grant. Amount **> 0** |
| `CarryOver` | Explicit prior-period/import carry. Amount **> 0**. No auto policy in 05A |
| `ManualAdjustment` | HR correction. Amount **≠ 0** (may be negative) |

No generic lookup table. No separate `LeaveAdjustment` entity.

---

## LeaveRecord (HR fact)

| Field | Notes |
|-------|--------|
| `Id` | Guid |
| `EmploymentId` | Owner |
| `LeaveTypeId` | Same-org, active at create |
| `StartDate`, `EndDate` | `DateOnly`, `StartDate <= EndDate` |
| `Amount` | `decimal` **> 0**, half-day quantum. **Authoritative** |
| `Status` | `LeaveRecordStatus` |
| `Note` | Optional |
| `CreatedAtUtc`, `CreatedByUserId` | |
| `CancelledAtUtc`, `CancelledByUserId`, `CancellationReason` | Set only by Cancel. Reason **required** |

No `LeaveRequest` in 05A. HR creates `LeaveRecord` directly.

05B may later finalize an approved request into a `LeaveRecord` without changing this table shape. Reserve conceptual `Origin` **only if** a single unused column is cheaper than a 05B migration; default freeze: **do not add Origin in 05A** unless implementation proves a zero-cost need. 05B can add it then. (Prefer no speculative column.)

### `LeaveRecordStatus`

`Recorded` | `Cancelled` only.

Do **not** add Pending, Approved, Rejected, ManagerApproved, HrApproved.

---

## Amount (day / half-day)

**Unit: day.** No hourly leave in 05A.

- CLR/`numeric`: precision **`numeric(6,1)`** (max 99999.9 days).
- Domain quantum: **multiples of 0.5** (0.5, 1.0, 1.5, …). Reject `0.33`, `0.25`, etc.
- `LeaveRecord.Amount` > 0.
- Entitlement `Entitlement`/`CarryOver` > 0; `ManualAdjustment` ≠ 0.

`0.5` means half-day **amount**. **No** Morning/Afternoon / AM/PM in 05A.

---

## Balance formula

**Never persist** `RemainingBalance`, `AnnualLeaveBalance`, or `yiDevir`-style fields.

For a type with `TracksBalance = true`, as of a date (Personnel Card = today in the existing clock convention used by the card; do not invent Property-local “today” in 05A):

```text
Balance(type) =
  SUM(LeaveEntitlement.Amount where LeaveTypeId = type)
  − SUM(LeaveRecord.Amount where LeaveTypeId = type AND Status = Recorded)
```

Cancelled records do not consume. Non-tracking types: no remaining line (usage still listed in history).

Negative remaining is allowed (visible); HR corrects with `ManualAdjustment`.

---

## Dates and calculation

- Store `StartDate`, `EndDate`, explicit `Amount`.
- UI **may suggest** inclusive calendar days (`End − Start + 1`) as a **hint**.
- HR **confirms** Amount. Persisted Amount is authority.

Do **not** assume Mon–Fri, Sat/Sun off, or public-holiday exclusion. No `WorkCalendar` in 05A.

### Invariants

- `StartDate <= EndDate`.
- Range must fit the Employment period: `StartDate >= Employment.StartDate`; if `Employment.EndDate` exists, `EndDate <= Employment.EndDate`.
- Record belongs to the Employment being managed (and that Employment belongs to the employee in the URL).
- **Ended Employment:** historical rows remain. HR **may** add entitlements, add records, and cancel records **if** leave dates sit inside that Employment period and the caller has `hr.leave.manage`. No new leave **outside** the employment period.
- **Scheduled Employment:** same period rule (pre-booked leave inside the future period is allowed).

---

## Overlap (frozen)

Two **Recorded** `LeaveRecord`s on the same **Employment** whose date ranges overlap (inclusive) are **rejected**.

This includes two half-days on the same calendar day (`0.5` + `0.5`). 05A has no time-of-day segment, so same-day split is **not** modeled.

Cancelled rows do not overlap.

Correction path: cancel, then create.

---

## Cancellation and correction

**Cancel Leave:** `Recorded` → `Cancelled`. Actor, UTC, **required** `CancellationReason`. Ordinary HR flow: **no hard delete**. Double cancel rejected.

**Entitlement correction:** new `ManualAdjustment` row (e.g. remaining 10, should be 12 → `+2`). Never `Balance = 12`.

---

## Default catalogue

Seed **per Organization** (not a global table).

| Code | SystemKind | TracksBalance (seed) |
|------|------------|----------------------|
| `annual` | Annual | true |
| `unpaid` | Unpaid | false |
| `sick` | Sick | false |
| `marriage` | Marriage | false |
| `paternity` | Paternity | false |
| `maternity` | Maternity | false |
| `bereavement` | Bereavement | false |
| `excuse` | Excuse | false |
| `administrative` | Administrative | false |
| `other` | Other | false |

i18n labels (UI), not semantic keys. Direction: TR Yıllık / Ücretsiz / Hastalık / Evlilik / Babalık / Doğum / Ölüm / Mazeret / İdari / Diğer; EN Annual / Unpaid / Sick / …; RU native.

**Initializer:** application service `EnsureDefaultLeaveTypes(organizationId)` — idempotent, inserts missing **system** codes only, does not revive hotel-deactivated system types, does not overwrite `Name`/`TracksBalance` if the code already exists.

Call from: development workforce seeder; first leave-type list/create for that org (lazy). There is **no** production Organization-create use case today; do **not** put tenant rows in the EF migration. Do **not** skip existing organizations.

---

## Personnel Card → İzinler

Placement (adapt to current keys; do not rename existing tabs):

| Order | TR (current / new) | EN | RU |
|-------|--------------------|----|-----|
| 1 | Genel bilgiler | General | … |
| 2 | Kimlik & iletişim | Identity & contact | … |
| 3 | Çalışma Bilgileri | Employment | … |
| 4 | Resmî bilgiler | Official information | … |
| 5 | Ödeme Bilgileri | Payment Information | … |
| 6 | **İzinler** | **Leave** | **Отпуска** |
| 7 | Geçmiş | History | История |

**Create flow:** tab is **hidden** until `mode === 'edit'` (Employee + Employment persist). Same as Geçmiş. Hire transaction must not include leave aggregates.

Requires `hr.leave.read` to see the tab (in addition to card access via `hr.employee.read`).

### Section A — İzin özeti

Balance-tracked types only: İzin türü, Net hareket (sum of entitlements), Kullanılan, Kalan.

Do not label Net hareket as “Hakediş” only if negative adjustments exist — use **Net hareket** / **Net movements** / **Чистое движение** for the entitlement sum, and **Kalan** for derived remaining.

### Section B — Hakediş ve düzeltmeler

Tarih, İzin türü, Kaynak, Miktar, Açıklama. Action: **Hakediş / Düzeltme ekle** (`hr.leave.manage`).

### Section C — İzin geçmişi

İzin türü, Başlangıç, Bitiş, Miktar, Durum. Include cancelled. **İzin ekle**, **İptal et** on Recorded (`hr.leave.manage`). No hard delete.

Command dialogs (Transfer / End Employment pattern), not a dirty-form dump on the tab.

---

## Permissions

| Code | Semantics |
|------|-----------|
| `hr.leave.read` | View tab, balances, entitlements, history |
| `hr.leave.manage` | Create record, create entitlement/adjustment, cancel |

No `hr.leave.approve` until 05B.

Policy pattern (match Workforce/HR employee): **read policy** = `hr.leave.read` **or** `hr.leave.manage`; **manage policy** = `hr.leave.manage`.

Register in `PermissionCatalog`. Seed onto existing HR templates: `hr-manager`, `hr-specialist`, `hr-corporate` (same `HumanResourcesPermissions` list). Permission = WHAT. Membership = WHERE (`EmployeeTenantGuard`).

`hr.employee.sensitive.read` is **not** required. `workforce.read` carries **no** leave payloads in 05A.

---

## Audit

Reuse `CreatedByUserId` / `CancelledByUserId` as **string** Identity user ids (`PersonnelProfileChange.UserIdMaxLength` = 450). UTC timestamps via existing clocks (`IWorkforceClock` / `TimeProvider`).

Do **not** dump leave into `PersonnelProfileChange`. Leave tables **are** the history. No event sourcing. No new generic audit framework.

---

## Explicitly out of HR-05A

**No** sidebar / Workforce subnav **İzin Yönetimi** page. Plan for HR-05B / follow-up (currently on leave, upcoming, department, property, type, status). Do not design that UI in this freeze.

**HR-05B — Leave Request & Approval (deferred):** `LeaveRequest`; employee self-service; manager approval; HR confirmation; rejection; cancellation request; notifications; mobile; HR-wide page. **No** manager/reporting-line model in 05A.

**Shift / Attendance (deferred):** schedule-aware charged days; weekly-off; public-holiday work; hourly leave; AM/PM or clock times; attendance effects; roster conflict. Do not couple 05A to nonexistent shift tables.

**Payroll:** out.

---

## NEEDS HR/LEGAL VALIDATION (not implemented)

WebİK’s 14/20/26 and age 18/50 are **reference evidence**, not HuGu policy.

- Seniority-band annual entitlement
- Age extras
- Automatic anniversary grants
- Statutory carry-over / expiry
- Termination unused-leave payout
- Protected leave legal durations
- SGK / eksik gün effects
- Yasal “izin defteri” print obligation

---

## Rejected WebİK patterns (do not implement)

- Leave usage as `pid-date → code` shift map
- `yiDevir` / remaining on Employee or Employment
- Matching annual leave by localized name
- Silent cell overwrite as cancel
- Hardcoded legal bands in domain
- `sabitTatilGunleri` Sat/Sun as leave calculator
- `1 gün = 7,5 saat` as leave unit
- FM mahsup / mesaiKodu / SGK eksik kod on LeaveType
- `OnLeave` / `Askıda` as `EmploymentStatus`

---

## Architecture guard (at implementation)

Allow production types `LeaveType`, `LeaveEntitlement`, `LeaveRecord` and the enums above.

Keep forbidden: `LeaveBalance` (stored remaining type), `LeaveRequest`, `ShiftAssignment`, payroll/government clients.

---

## Non-goals of this freeze document

No backend, frontend, migration, or database change is authorized by reading this file alone. Implementation starts only from the plan + this freeze, after the planned stop for review if still required by the implementation-plan header.
