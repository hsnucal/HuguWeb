# HR-05B — Leave Request & Approval Implementation Plan

> **Status:** Accepted / Completed — Product Owner runtime acceptance (2026-08-31)  
> **Domain:** [HR-05B-Leave-Request-Approval.md](HR-05B-Leave-Request-Approval.md) — Accepted / Frozen  
> **Baseline:** `ff0ba1e`  
>
> Domain freeze accepted (2026-08-31). Implementation slices A–C delivered with PO findings 02–05. HR-05B closed after PO manual acceptance.

---

## Goal

Deliver leave request + approval that:

1. Preserves HR-05A `LeaveRecord` / balance / direct HR entry  
2. Uses HR-06 schedule for **preview only**  
3. Uses AUTH-02 Department scopes for *where* and new leave permissions for *what*  
4. Remains consumer-neutral for future mobile  

---

## Dependencies / blockers

| Dependency | Status |
|------------|--------|
| HR-05A Accepted / Completed | Done |
| HR-06 Accepted / Completed | Done |
| AUTH-02 Accepted / Completed | Done |
| EmployeeAccountLink | Exists; unused by leave — enable for self-service |
| Domain freeze Q1–Q5 | **Accepted** — see domain doc §29 Accepted answers |

Architecture note: `ProductionAssemblyGuardTests` currently forbids type name `LeaveRequest`. Accepting HR-05B requires an intentional allow-list update when implementation starts.

---

## Delivery slices

### A — Domain + persistence (+ approval core) — **Accepted**

- `LeaveRequest`, `LeaveRequestDecision` aggregates  
- Migration: tables + indexes + checks  
- `LeaveRecord.SourceLeaveRequestId` nullable unique FK  
- Status / stage enums; Amount quantum reuse  
- Update architecture guard for `LeaveRequest`  
- Approval core use cases (dept/HR/reject/withdraw/cancel-approved) testable without API  

**Out:** Full API surface, UI.

**Schedule preview helper:** implemented in Slice B (`LeaveSchedulePreview`).

**Permission templates:** Slice B seeded — see below.

### B — Application / API (create & read + approval APIs) — **Accepted**

- Self-service `/api/hr/my/leave-requests` (+ preview, withdraw) via `employee_id` claim from EmployeeAccountLink  
- Management `/api/hr/leave-requests` list/detail with AUTH-02 department filtering on **persisted Assignment**  
- Department approve / reject / HR final approve / cancel-approved endpoints  
- Schedule preview (HR-06): Scheduled / RestDay / Unscheduled; SuggestedAmount; ScheduleIncomplete warning  
- Soft balance overrun warning  
- Permission policies: `HrLeaveRequest`, `HrLeaveApprove`; HR templates include `hr.leave.approve`; `DepartmentSchedulerPermissions` includes leave read+approve; `EmployeeLeaveSelfServicePermissions` = request only (no system role template yet — assign via role admin)  

**Out:** UI.

### C — Management UI + Self-Service UI — **Accepted / Completed**

Delivery naming for this increment (product Slice C): management + employee self-service UI.

- Route `/app/workforce/leave-management` (İzin Yönetimi) — tabs, filters, detail, dept/HR approve, reject, cancel-approved  
- Route `/app/my/leave` (İzinlerim) — balances, create + preview, withdraw Pending  
- Minimal self-service catalog: `GET /api/hr/my/leave` (active types + balances; `HrLeaveRequest`)  
- TR/EN/RU localization; focused frontend pure-logic tests  

**Deferred (Slice D / Personnel Card):** read-only Talepler on Personnel Leave tab — not blocking Slice C.

### Legacy plan letter C — Approval transaction — **Merged into A + B**

Core transitions landed in Slice A; HTTP surface in Slice B. 

### D — Schedule preview — **Delivered in B**

- Per-day Scheduled / RestDay / Unscheduled summary DTO  
- SuggestedAmount calculation  
- Incomplete flag; re-preview on approve  
- Guarantee: post-approval schedule edits do not mutate LeaveRecord.Amount  

### E — Authorization — **Delivered in B**

- Permissions: `hr.leave.request`, `hr.leave.approve` (+ templates)  
- Wire `MembershipDepartmentAccess` for approve/list  
- Property-wide `hr.leave.manage` stage bypass rules per freeze  
- Tenant / org / property guards  

### F / G — Management + Self-service UI — **See product Slice C above**

### H — Personnel Card integration — **Deferred**

- Read-only Talepler section under Leave tab  
- No approval actions on card  

### I — Tests / localization / docs

- Full test matrix from domain doc  
- resx + frontend i18n  
- Mark domain/implementation Accepted after PO  

---

## Suggested sequencing

```text
A → B → product C (UI) → H (Personnel Card) → I finalize
```

Minimum shippable vertical:

`A + B + product C` (management + self-service + HR/dept approval)  

Then `H` for Personnel Card Talepler.

---

## Non-goals (this plan)

- Hourly leave  
- Attachments / substitute  
- Notification infrastructure  
- Mobile app UI  
- Reporting-line hierarchy  
- Configurable workflow engine  
- Public holiday calendar  
- Attendance (HR-07)  
- Changing HR-05A negative-balance philosophy without PO  

---

## Migration expectations (when unblocked)

Likely **one** Workforce migration:

1. `LeaveRequests`  
2. `LeaveRequestDecisions`  
3. `LeaveRecords.SourceLeaveRequestId` (+ unique filtered index)  

No Identity migration unless permission seed-only (permissions are catalog/code, not schema).

---

## Verification bar (implementation phase)

- `dotnet build` / `dotnet test` — 0 errors, 0 warnings  
- Frontend lint + build  
- Explicit tests: double approve, idempotency, cross-dept reject, schedule incomplete, RestDay exclusion, Unscheduled warning, cancel-approved atomicity  

---

## Exit criteria

| Gate | Owner |
|------|-------|
| Domain freeze Accepted | CTO + PO |
| Implementation slices A–I Done | Engineering |
| Manual PO acceptance | PO |
| Docs status → Accepted / Completed | Engineering |

Domain acceptance complete. Proceed slice-by-slice; Slice A = domain + persistence + approval core (no full API/UI).

---

## PO Acceptance Finding 01 (2026-08-31) — technician “İzinlerim” missing

**Status:** Corrected (seed/auth fixture) — awaiting re-verification by PO.

**Observation:** `maintenance.technician@localhost` sidebar showed Ana sayfa + Teknik Servis only; **İzinlerim** missing.

**Root cause (Case A + Case C — not a frontend nav bug):**

1. Role `maintenance-technician` granted only `maintenance.read` + `maintenance.resolve` — **no** `hr.leave.request`. Sidebar permission guard correctly hid İzinlerim.
2. Development seed never created `EmployeeAccountLink` for any persona — self-service APIs would still fail after permission alone.

**Frontend:** No change. Visibility remains `session.permissions` contains `hr.leave.request`.

**Correction (dev seed / system templates only):**

- Added system role template `employee-leave-self-service` (`hr.leave.request` only).
- Assigned that role **alongside** `maintenance-technician` for the technician persona (no email-based runtime identity).
- Seeded `EmployeeAccountLink` → workforce employee `DEV-2001` / `DevelopmentEmployeeId` (Teknik Servis, open Employment).
- Requires API restart so `DevelopmentUserSeeder` runs; then **fresh login** (security stamp refresh).

**Not Completed:** Whole HR-05B remains open for further PO acceptance.

---

## PO Acceptance Finding 02 (2026-08-31) — Demo User ↔ Employee mapping cleanup

**Status:** Corrected (dev seed / fixtures) — awaiting PO manual re-test.

**Observation:** `maintenance.technician@localhost` had `hr.leave.request` and sidebar **İzinlerim**, but `/app/my/leave` returned account-link error. Live DB had an orphan `EmployeeAccountLink` to missing `DevelopmentEmployeeId` (`…0201`); only unrelated employees `1001`/`1003` existed. `hr.manager@localhost` had no link. Workforce seed skipped personnel when workplace already existed → stale demo drift.

**Root cause:** Finding 01 linked technician to `DEV-2001` / `DevelopmentEmployeeId`, but that Employee row was never recreated after older seed versions left different personnel. Identity bridge was incomplete/orphan. ApplicationUser ≠ Employee remained correct; runtime already used `EmployeeAccountLink` only.

**Correction (DEVELOPMENT-ONLY):**

1. Targeted operational personnel reset (`DevelopmentOperationalPersonnelReset`) — FK-ordered clear of employees/employments/assignments/leave/schedule/profile ops data; preserves Organization/Property/Department/Position/auth users/roles. Guarded with `isDevelopment`.
2. Deterministic persona fixtures (`DevelopmentPersonaEmployeeFixtures` + `DevelopmentPersonaEmployeeSeeder`): own Employee + open Employment + Primary Assignment + fixed PersonnelNumbers (`DEMO-TECH-01`, `DEMO-HR-01`, …) per employee persona.
3. Catalog + `DevelopmentUserSeeder`: explicit `LinkedEmployeeId` / `LinkedAccountLinkId`; corrects wrong/orphan links; non-employee personas (`dev@localhost`, `hr.corporate@localhost`) stay unlinked.
4. Seed order: workforce persona employees **before** user/link seed.
5. `hr.manager` (and other employee personas) get explicit `employee-leave-self-service` (`hr.leave.request`) — **not** implied by `hr.leave.manage`.
6. Technician and HR manager map to **different** Employees; no runtime email/PersonnelNumber identity matching.

**Not Completed:** HR-05B still open for PO acceptance.

---

## PO Acceptance Finding 03 (2026-08-31) — Leave approval queue missing

**Status:** Corrected (dev seed / roles / AUTH-02 scopes) — awaiting PO re-test.

**Observation:** Technician created Pending/Department leave (ENG / DEMO-TECH-01). `maintenance.manager` and `hr.manager` saw no approval queue.

**Root cause (before fix):**

- **A + B + F (primary for maintenance.manager):** Role `maintenance-manager` had only `maintenance.*` (+ Finding 02 self-service request). Missing `hr.leave.read` / `hr.leave.approve` → sidebar hid İzin Yönetimi; `HrLeaveRead` API denied list.
- **C:** Not missing ENG access — membership was Property-wide (zero scopes = all depts). Still seeded **ENG** scope explicitly for intended department-approver WHERE.
- **D / E / G:** Request Assignment correctly ENG / Ankara; management query does not hide Department-stage.
- **H:** List semantics intentionally include Department-stage for any caller with leave read + workplace access. HR with `hr.leave.manage` **can see** Departman bekliyor but **cannot** final-approve until Pending/Hr (stage guard). If HR UI looked empty, likely wrong tab/filter or session; backend list allows Department-stage.

**Intended ENG department approver persona:** `maintenance.manager@localhost` (not HR-first).

**Fix (seed / system templates only):**

- System role `department-leave-approver` (`hr.leave.read` + `hr.leave.approve`).
- Assigned alongside `maintenance-manager` (+ existing self-service).
- AUTH-02 department scope codes `["ENG"]` for that membership.
- No domain / topology / migration change.

**Not Completed:** HR-05B remains open.

---

## PO Acceptance Finding 04 (2026-08-31) — Leave Management UX redesign

**Status:** Corrected (frontend UX only) — awaiting PO visual re-test.

**Observation:** End-to-end approval flow worked, but Leave Management felt weak: nested approve/reject modals, repeated list actions, generic “Onayla” for department stage, FinalAmount detached in a second modal, heavy warnings/scroll.

**Correction (UI only — no domain/API/migration change):**

- Single approval workspace: detail dialog; no nested approval/reject modals.
- List rows: **İncele** only.
- Department primary: **İK'ya Gönder** / Send to HR / Отправить в HR (+ inline hint; no LeaveRecord).
- HR stage: inline **Nihai onay** with FinalAmount default = RequestedAmount; SuggestedAmount advisory; approve from same panel.
- Reject: inline panel in detail.
- Approved cancel: one compact destructive confirm (allowed exception).
- Compact balance/schedule metrics; schedule day list collapsed; compact notices; decision timeline without actor GUIDs.
- Wider compact dialog (~880px), sticky header/footer, footer left Close / right workflow actions, one primary per state.

**Not Completed:** HR-05B not Completed.

---

## PO Acceptance Finding 05 (2026-08-31) — Date picker, submit button, leave defaults, Personnel layout

**Status:** Corrected — awaiting PO visual/runtime re-test.

**Observation:**

- New Leave Request date fields felt unusable (no shared calendar enabled; typing-only without picker).
- Submit primary was full-width (`Button` primary defaults to `layout="block"`) and still labeled “Yeni izin talebi”.
- Product wanted configurable request defaults: Paternity 10 / Birthday 1 / Bereavement 3 without name/code hardcoding in React.
- Personnel directory filter grid regressed: Finding 04 narrowed `.hrFilters` to 4 columns while Personnel has 6 controls → “İşe giriş bitişi” / “Sütunlar” orphaned.

**Root causes:**

1. **Date:** Shared `DateField` already supports `calendar` (native `showPicker` + DD.MM.YYYY). My Leave create dialog omitted `calendar`.
2. **Submit button:** Primary `Button` default `layout="block"` → 100% width in dialog footer.
3. **Defaults:** No `LeaveType.DefaultRequestAmount` configuration field.
4. **Personnel layout:** CSS leakage via shared `.hrFilters` column template change for Leave Management filters.

**Fix:**

- Enable shared calendar on leave Start/End (and Personnel directory date filters).
- Footer: content-sized `[Vazgeç] … [Talebi gönder]` / Submit request / Отправить запрос.
- Domain: nullable `LeaveType.DefaultRequestAmount` (0.5 quantum, optional). Migration `AddLeaveTypeDefaultRequestAmount`. Seed via SystemKind for Paternity=10 / Bereavement=3; Birthday as custom (`SystemKind=null`, code `birthday`, default=1). Precedence: user edit > type default > schedule suggestion.
- Leave Type admin field: Varsayılan talep süresi.
- Isolate grids: `.hrFilters` (Personnel 6-col) vs `.leaveMgmtFilters` (Leave Management 4-col).

**Semantics:** `DefaultRequestAmount` is request UX configuration only — not entitlement, balance, statutory engine, or HR FinalAmount.

**Migrations (HR-05B):** two — foundation + `AddLeaveTypeDefaultRequestAmount`.

---

## Completion (2026-08-31)

**Status:** Accepted / Completed after Product Owner runtime acceptance.

Included PO findings 02–05 (persona EmployeeAccountLink seed, department-leave-approver / ENG visibility, Leave Management UX, date picker + DefaultRequestAmount + Personnel filter layout).

Personnel Card Talepler remains deferred as a non-blocking follow-up.

Domain freeze decisions are unchanged.
