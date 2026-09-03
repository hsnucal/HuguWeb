# ADR-011: Puantaj Domain Model

> Copy of the [ADR Template](ADR-TEMPLATE.md) filled for HR-07. **Accepted.**

---

## Status

**Accepted** — Product Owner + CTO (2026-09-03)

HR-07A (backend foundation) is authorized and implemented. HR-07B (monthly grid + sidebar) is implemented and **PO-accepted** (2026-09-03). The HR-07 operational MVP is **Completed / Accepted**. PDKS/punch, period lock, official holiday engine, half-day/Partial, and overtime/payroll/SGK remain deferred.

---

## Context

HuGu already has:

- **Plan:** `ScheduleEntry` (`Shift` | `RestDay`; Unscheduled = no row) and Property-scoped `ShiftDefinition` (HR-06).
- **Absence fact:** `LeaveRecord` (`Recorded` | `Cancelled`); pending `LeaveRequest` is workflow only (HR-05A/05B).
- **Workplace:** `Employee` → `Employment` → `Assignment` → `Department`/`Position` → `Property.TimeZoneId`.
- **Scope:** AUTH-02 department membership; explicit `HuGuWeb.ActiveProperty`; no silent Property fallback.

WebİK TimeCore (reference snapshot only) shows a monthly **Aylık Puantaj** grid that **reads the same cell map as Shift Atama** (`pdks_shift_data`), paints leave as a cell code, overlays punches by fading assigned shifts with no `hareket`, and has **no** Puantaj period lock or cell audit. That is a useful capability reference and an architecture to **reject**.

Product freeze for this slice:

- Puantaj is a **top-level operational module**, not a Personnel Card tab.
- Vardiya Planlama = planned work. Puantaj = **accepted** payroll-relevant attendance result.
- Do **not** reuse or overwrite `ScheduleEntry` as the actual result.

HR-07 MVP must serve hotel operations (monthly grid, leave + rest + unresolved + manual correction + audit + department scope) without payroll engines, devices, or microservices.

---

## Problem

How should HuGu persist and present monthly attendance **without**:

- collapsing plan and actual,
- painting leave onto the roster,
- storing unnecessary derived copies of shift/leave,
- blocking future PDKS punches,
- calculating overtime pay or SGK declarations,
- inventing a Turkish official-holiday engine?

---

## Decision

We will:

### 1. Keep Puantaj ≠ Schedule

`ScheduleEntry` remains plan-only. Puantaj is a **read model** (`AttendanceDay` conceptually) plus a **sparse write model** for human-accepted exceptions.

### 2. Treat Puantaj as a top-level operational module

Primary navigation direction:

```text
Ana Sayfa → Personel → Puantaj → Vardiya Planlama → İzin Yönetimi
```

Personnel Card stays master-data. Implementation of the sidebar split waits for an authorized UI slice; the domain boundary is frozen now.

### 3. Persist corrections, not every day (Option B)

**MVP physical model:**

- Derive each Employment × Property-local `DateOnly` from `ScheduleEntry` + Recorded `LeaveRecord`.
- Persist `AttendanceCorrection` only when HR/manager asserts a result different from (or pinning) that derivation, including explicit Absent and explicit Clear.
- Persist append-only `AttendanceCorrectionChange` (previous/new result, actor, UTC, required note).

**Reject Option A for MVP** (one row per employment per date from month-open). Full-day rows are reserved for **period lock materialization** (deferred).

### 4. LocalDate ownership

The business day is `DateOnly` in `Property.TimeZoneId`. Technical timestamps are UTC `DateTimeOffset`. Timezone is **not** copied onto correction rows. Future punches are UTC instants mapped to LocalDate via the same Property zone. Overnight planned shifts remain attributed to the **start** `ScheduleDate` (HR-06).

### 5. Source precedence (accepted result)

Highest wins; **sources are never silently mutated**:

1. **Manual correction** (persisted current `AttendanceCorrection`)
2. **Future punch observation** (enum reserved; not in MVP; never written into `ScheduleEntry`)
3. **Recorded `LeaveRecord` covering the local date** → accepted **Leave**, `Source = Leave`
4. **Schedule RestDay** → accepted **RestDay**, `Source = Schedule`
5. **Schedule Shift** → accepted **Worked**, `Source = Schedule`, **provisional** (`IsProvisional`). A planned Shift is not observed attendance.
6. **No schedule** → accepted **Unresolved**

Pending leave requests do not participate. Cancelled leave records do not participate.

Manual Worked and schedule-derived Worked share accepted kind `Worked`. Provenance (`Source` + `IsProvisional`) distinguishes them. `AcceptedWorkedMinutes` is null in HR-07A.

Overlays, not competing statuses:

- Plan RestDay + accepted Worked ⇒ worked-on-rest (HR-08 input), not “Holiday.”
- Future official holiday flag + Worked ⇒ worked-on-holiday, not status=`Holiday`.
- Absent is **explicit** (manual in MVP). Unscheduled empty is **Unresolved**, never inferred Absent or RestDay.

Accepted-result enum (minimal, explicit — not a string soup):

`Unresolved | Worked | RestDay | Leave | Absent`

`Partial` / hourly physics deferred. `OfficialHoliday` is a **flag**, not a member of this enum.

### 6. Audit model

Domain-owned history (foundation Audit pattern B), same spirit as `ScheduleEntryChange`. Required note. No generic JSON dump. No silent overwrite.

### 7. Authorization / scope

New permission codes (names follow repository convention):

- `hr.attendance.read` — HR templates + `department-scheduler` (least privilege; AUTH-02 department-narrowed)
- `hr.attendance.manage` — HR templates only; not auto-granted to department-scheduler
- `hr.attendance.close` — **catalogued for future lock; not granted to HR or department templates**

Department-aware like `hr.schedule.*`: Assignment department **on that date** ∩ AUTH-02 allowed departments. Backend authoritative. Active Property required.

### 8. Future punch extensibility

Do not add `AttendancePunch` now. Keep `Source` on the read model as `Schedule | Leave | Manual` with room for `Punch`. Corrections remain valid when punches arrive. Manual still overrides observation.

### 9. No payroll calculation in HR-07

No wage, OT pay, 30-day SGK capping, Elektra/Logo, or TP/SGK persisted ledgers. HR-07 may expose planned net minutes, accepted result, rest/leave/absent/unresolved counts. HR-08 consumes facts.

### 10. Period lock

**Not in MVP.** Direction: future `Open` / `Locked`; lock materializes derived+corrected days so later plan/leave edits cannot rewrite history. Payroll should eventually consume locked months only. Until a consumer exists, HR may correct past months with audit.

---

## Alternatives Considered

| Alternative | Outcome | Reason |
|-------------|---------|--------|
| Reuse `ScheduleEntry` as Puantaj (WebİK cell map) | **Rejected** | Collapses plan/actual; leave-as-cell already rejected in HR-05A/HR-06 |
| Option A: persist every AttendanceDay in MVP | **Rejected for MVP** | Duplicates plan/leave; month-generation/recalc cost; lock can materialize later |
| Option B: derive + persist corrections | **Accepted** | Sparse, auditable exceptions; sources stay SoT |
| Single giant status enum including Holiday and Partial | **Rejected** | Holiday is overlay; Partial needs leave-time physics HuGu does not have |
| Infer Absent from empty cells | **Rejected** | Hotels 24/7; empty = Unscheduled/Unresolved (HR-06 lesson) |
| Implement PDKS / punches in HR-07 | **Rejected** | Device integration is a later bounded context |
| Implement period lock in HR-07 | **Deferred** | No payroll consumer; WebİK lock on this screen not evidenced |
| Official holiday engine in HR-07 | **Deferred** | No current calendar entity; do not invent gazetted TR rules |
| Puantaj as Personnel Card tab | **Rejected** | Operational monthly workspace; card stays master-data |
| Frontend-only department filter | **Rejected** | AUTH-02 + backend remain authoritative |

---

## Consequences

### Positive

- Plan, leave, and accepted result remain three facts with deterministic merge.
- Open-month leave approval updates Puantaj without a backfill job.
- Manual corrections are historically stable and reviewable.
- Punch/lock/payroll can attach later without rewriting `ScheduleEntry`.
- Query shape stays a bounded month join — compatible with 100+ hotels if department-filtered.

### Negative

- Editing last month’s roster **will** change derived Puantaj until a lock snapshot exists (must be explained in UX).
- Leave **Amount** vs painted calendar span remains a known gap (no charged-day engine).
- Readers must understand Unresolved ≠ Absent ≠ RestDay.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Operators “correct” plan on Puantaj and expect roster to change | UI copy: correction does not edit Vardiya Planı; link to schedule |
| Historical drift before lock | Warn on past-month edits; lock ADR when payroll starts |
| Permission explosion | Three codes only; close unused until lock |
| Copying WebİK 30-day SGK into totals | Explicit non-goal; operational counts only |
| Snapshot files leaking into git | Keep WebİK tree outside the repo (already) |

---

## Revisit Conditions

- Payroll or SGK consumer requires frozen months → implement lock + Option A materialization.
- PDKS vendor selected → add `AttendancePunch`; revisit suggestion rules (scheduled without punch).
- Charged-day leave engine accepted → change leave paint vs RestDay.
- Official holiday catalogue accepted → add overlay flag only.
- Measured grid size (property-wide, no department filter) exceeds UX/query budget → mandatory department paging.

---

## Deferred items

- `AttendancePunch` persistence and device adapters  
- `hr.attendance.close` behavior and unlock policy  
- Official holiday catalogue  
- Hourly / half-day cell split  
- Overtime pay (HR-08)  
- Wage / SGK / e-declaration  
- Bulk copy on the Puantaj screen  
- Employee self-service timesheet  

---

## Date

2026-09-02 proposed; **Accepted 2026-09-03**

---

## Related Documents

- [HR-07-PUANTAJ-DISCOVERY.md](../../product/hr/HR-07-PUANTAJ-DISCOVERY.md)
- [HR-06-Shift-Work-Schedule.md](../../product/hr/HR-06-Shift-Work-Schedule.md)
- [HR-05A-Leave-Foundation.md](../../product/hr/HR-05A-Leave-Foundation.md)
- [ADR-008-Authorization-Strategy.md](ADR-008-Authorization-Strategy.md)
- [ADR-010-Database-Managed-Authorization.md](ADR-010-Database-Managed-Authorization.md)
- [TIME_AND_TIMEZONE.md](../foundation/TIME_AND_TIMEZONE.md)
- [AUDIT.md](../foundation/AUDIT.md)
- [TENANCY.md](../foundation/TENANCY.md)
- [DEPARTMENT_MEMBERSHIP_SCOPE.md](../../security/authorization/DEPARTMENT_MEMBERSHIP_SCOPE.md)
