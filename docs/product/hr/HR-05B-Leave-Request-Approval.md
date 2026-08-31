# HR-05B — Leave Request & Approval / İzin Talebi & Onay

> **Status:** Accepted / Frozen  
> **Type:** Domain freeze (Accepted) — implementation proceeds via incremental slices  
> **Baseline:** `ff0ba1e` — `feat(hr): complete shift planning`  
> **Accepted:** CTO + Product Owner (2026-08-31)  
>
> Does **not** supersede HR-05A, HR-06, AUTH-02, or earlier Accepted HR domains.  
> WebİK remains capability reference only — not HuGu schema, naming, or UI to copy.  
> Snapshot path `C:\Users\hsnuc\Desktop\ik.webik.com.tr` is **not** in this repository.

---

## 0. Accepted / Frozen decisions (CTO + PO — 2026-08-31)

These decisions are **final** for HR-05B. Discovery narrative below remains historical context; where wording conflicts, **this section wins**.

1. Approval topology: Department approval → HR final approval.
2. Department approval does **not** create `LeaveRecord`.
3. `LeaveRecord` is created **only** on HR final approval.
4. HR final approval + `LeaveRecord` creation happen atomically in **one** transaction.
5. Rejection at either approval stage ends the workflow as `Rejected`.
6. `LeaveRequest` ≠ `LeaveRecord`.
7. `LeaveRecord` remains the authoritative leave fact.
8. Direct HR `LeaveRecord` entry from HR-05A remains supported.
9. `LeaveRequest` belongs to `Employment`.
10. `LeaveRequest` persists historical `AssignmentId`.
11. `AssignmentId` **MUST NOT** be accepted from the client.
12. Backend resolves Primary Assignment using `StartDate`.
13. Entire requested date range must remain inside the **same** Assignment interval.
14. Cross-Assignment / cross-department range is rejected; user must submit separate requests.
15. Self-service requires **both** `EmployeeAccountLink` and `hr.leave.request`.
16. Department approval requires `hr.leave.approve` + AUTH-02 Department scope.
17. HR final approval uses existing HR authority: `hr.leave.manage` + appropriate tenant/property context.
18. Permission answers WHAT; Department scope answers WHERE.
19. Schedule preview: Scheduled = chargeable candidate; RestDay = 0; Unscheduled = incomplete/unknown.
20. Unscheduled does **not** hard-block request creation.
21. `SuggestedAmount` is derived only from known Scheduled days.
22. If Unscheduled exists: request/approval UI later shows explicit schedule-incomplete warning.
23. `FinalAmount` is explicitly confirmed at HR final approval (input to approve use case; frozen into `LeaveRecord.Amount` — **not** a persisted column on `LeaveRequest` in Slice A).
24. Schedule changes after approval never mutate `LeaveRecord.Amount`.
25. Request amount uses 0.5 quantum.
26. Half-day amount is supported; **no** AM/PM semantics.
27. Hourly leave is **DEFERRED**.
28. Balance overrun: soft warning only; do **not** hard-block negative balance.
29. Pending request is immutable; wrong request = withdraw/cancel and submit new.
30. Pending can be withdrawn by allowed actor.
31. Approved cancellation is a separate authorized action.
32. Approved cancellation: marks request `Cancelled`, cancels generated `LeaveRecord`, preserves history, atomic.
33. Rejected request: no `LeaveRecord`, immutable history, does not consume balance.
34. Overlap: active Pending/Approved requests block overlapping active requests for same Employment.
35. Existing Recorded `LeaveRecord` overlap remains authoritative blocker.
36. Rejected/Cancelled requests do not block future requests.
37. `LeaveRecord.SourceLeaveRequestId` is nullable + UNIQUE.
38. Not every `LeaveRecord` has a request.
39. One request can create at most one `LeaveRecord`.
40. Approval retry/double click must never create duplicate `LeaveRecords`.
41. No generic workflow/BPM engine.
42. No direct-manager/reporting-line domain in this slice.
43. No hourly leave.
44. No attachments.
45. No substitute employee.
46. No public holiday engine.
47. No notifications infrastructure.
48. No Attendance/PDKS.
49. No payroll.
50. No mobile UI.

**Persisted `LeaveRequest` fields (frozen):** Id, EmploymentId, AssignmentId, LeaveTypeId, StartDate, EndDate, RequestedAmount, Status, ApprovalStage, Reason (optional), CreatedByUserId, CreatedAtUtc, UpdatedAtUtc.

**Link model:** FK is only `LeaveRecord.SourceLeaveRequestId` → `LeaveRequests.Id` (Restrict). No `LeaveRequest.LeaveRecordId` / `FinalAmount` / `SchedulePreviewIncomplete` columns in Slice A.

**LeaveType inactive after submit:** historical Pending/Hr requests remain reviewable; final approval may proceed if other validations pass. New creates still require active type.

**Concurrency (Slice A):** no RowVersion on leave entities (not project convention here). Guarantees via server-side Status/ApprovalStage checks + unique filtered index on `SourceLeaveRequestId` + single DB transaction.

**Schedule preview helper:** deferred to Slice B/D; behavior frozen above.

**Permission templates:** `hr.leave.request` / `hr.leave.approve` registered in catalog; role-template assignment deferred to authorization slice (ambiguous for department-manager / employee portal roles).

**Self-service identity (Slice B):** resolve `ActorContext` / claims → `EmployeeAccountLink.UserId` → `EmployeeId` → current Employment. No email/personnel-number matching.

---

## 1. Goal

Answer:

> How does an employee request leave, how is that request evaluated/approved, and when does it become an authoritative `LeaveRecord`?

Hotel operations need:

| Actor | Need |
|-------|------|
| Employee | Submit leave intent for linked Employment |
| Department-authorized approver | Review within AUTH-02 Department scopes + leave-approve permission |
| HR | Oversee / final approve / manage according to permissions |
| System | On approval, create **one** authoritative `LeaveRecord` (HR-05A fact) |

**Not** a generic BPM/workflow engine. Smallest explicit lifecycle.

---

## 2. Slice identity

| | |
|--|--|
| Slice | **HR-05B** Leave Request & Approval |
| Builds on | **HR-05A** (Accepted / Completed) |
| Consumes | **HR-06** schedule states for **preview only** |
| Authorization | **AUTH-02** Department scopes for *where*; new leave permissions for *what* |
| Deferred siblings | Notifications, mobile UI, hourly leave, attachments, payroll, Attendance (HR-07) |

Planning map: [README.md](README.md).

---

## 3. Current HuGu state (must preserve)

### HR-05A (Accepted)

```text
Organization → LeaveType*
Employee → Employment* → LeaveEntitlement* | LeaveRecord*
```

| Invariant | Detail |
|-----------|--------|
| Ownership | Leave on **Employment**, not Employee |
| `LeaveRecord` | Authoritative fact: dates + `Amount` (day / 0.5 quantum) |
| Status on record | `Recorded` \| `Cancelled` only — **no** Pending/Approved on fact table |
| Balance | Derived only; **negative remaining allowed** |
| Direct HR entry | Remains valid forever — not every record comes from a request |
| Overlap | Two **Recorded** ranges on same Employment cannot overlap |
| Semantics | `SystemKind` / `Code` — never localized `Name` |
| No Origin column yet | 05A deferred Origin; 05B may add nullable link |

### HR-06 (Accepted)

| State | Meaning |
|-------|---------|
| Scheduled | `ScheduleEntry.Kind = Shift` |
| RestDay | Explicit planned not-to-work |
| Unscheduled | **Absence of row** — unknown, not RestDay |

Authorization for schedule uses **target-date Primary Assignment**. Leave must **not** store leave inside `ScheduleEntry` and must **not** add `ScheduleEntry.Kind = Leave`.

### AUTH-02 (Accepted)

- `UserMembership` = Organization + optional Property  
- `UserMembershipDepartmentScope` = 0..N Department narrowing (0 = Property-wide)  
- Permission = WHAT; Department scope = WHERE  
- Having Department scope **does not** imply leave approval rights  

### EmployeeAccountLink (exists, unused by leave)

```csharp
// Identity: UserId ↔ EmployeeId (unique both ways)
```

Ready for self-service resolution. Leave APIs today are HR-actor only (`hr.leave.read` / `hr.leave.manage`).

---

## 4. WebİK evidence (reference only)

Source: admin FE snapshot (`pdks_app` / `sync`, download 2026-08-24). **No employee-portal form bundle in snapshot.**

| Topic | Classification | Notes |
|-------|----------------|-------|
| Approval queue | **CONFIRMED** | `pdks_onay_bekleyen`; title covers shift/izin |
| Portal day fields | **CONFIRMED** (consumed) | `baslangic`, `bitis`, `gunSayisi`, `kod`, `sebep`, `asama`, `talepId` |
| Stages (`asama`) | **CONFIRMED** | Non-`ik` → Departman onayı; `ik` → İK onayı |
| HR approval | **CONFIRMED** | Stage `ik`; admin can approve any stage |
| Department filter | **PARTIAL** | `kullanici.departmanlar` ∩ `per.departman` — not amir hierarchy |
| Manager hierarchy | **NOT CONFIRMED** | No reporting-line proven for leave |
| Rejection | **CONFIRMED** | `status/durum: reddedildi` + optional `rejectionReason` |
| Request reason (`sebep`) | **CONFIRMED** | Read in queue |
| Approver free-text note | **PARTIAL** | Reject reason; cancel `kararNotu`; approve note not found |
| Day vs hourly | **CONFIRMED** | Separate flows / stores |
| Day leave in shift cells | **CONFIRMED** | HuGu **rejects** this model |
| Hourly separate ledger | **CONFIRMED** | “Vardiya etkilenmez” |
| Employee self-service form | **PARTIAL** | Referenced in copy; **form JS absent** |
| Balance block on create | **PARTIAL** | Warn on some hourly; toplu izin no bakiye check |
| Attachments / substitute | **NOT CONFIRMED** | |
| Request edit | **NOT CONFIRMED** | |
| Cancellation (employee) | **PARTIAL** | Saatlik İK cancel found; employee day cancel not found |
| Notifications | **CONFIRMED** | Portal notification summary includes izin/saatlik |
| Overlap of requests | **PARTIAL** | Heat-map density, not request-vs-request validation |

**HuGu does not copy:** shared leave/shift cells, name-based annual matching, mutable stored remaining, generic BPM.

---

## 5. Aggregate model — LeaveRequest

### Ownership (recommended freeze)

```text
Employee
  └── Employment
        ├── LeaveEntitlement*
        ├── LeaveRecord*          (authoritative facts — HR-05A)
        └── LeaveRequest*         (workflow intent — HR-05B)
              └── LeaveRequestDecision*  (append-only)
```

**LeaveRequest belongs to Employment**, not bare Employee.

### Proposed fields (conceptual) — superseded by §0 frozen field list

Historical discovery listed optional `FinalAmount`, `SchedulePreviewIncomplete`, and `LeaveRecordId` on the request. **Accepted freeze:** those are **not** persisted on `LeaveRequest`. Final amount is an HR-approve input written only to `LeaveRecord.Amount`. Link is solely `LeaveRecord.SourceLeaveRequestId`.

### AssignmentId rule (Accepted)

- Client **must not** supply `AssignmentId`.
- At **submit**, resolve Primary Assignment covering `StartDate` via `PrimaryAssignments.Covering` (same family as HR-06).  
- Persist that `AssignmentId` on the request.  
- Require the **same** Assignment to cover **every** date in `[StartDate, EndDate]` (Assignment interval, not merely DepartmentId match).  
- If any date is uncovered or covered by a different Primary Assignment → reject with `leave-request-cross-assignment-range` (or `leave-request-assignment-not-found` when none covers StartDate).  
- Department authorization for the request uses that Assignment’s Department (plus AUTH-02 scopes) in later slices.  
- Do **not** auto-split; do **not** use current/first/default Assignment fallback.

---

## 6. Request vs Record

| Concept | Role |
|---------|------|
| **LeaveRequest** | Workflow / intent / approval lifecycle |
| **LeaveRecord** | Authoritative recorded leave fact (balance consumer) |

**Relationship (Accepted):**

```text
LeaveRequest 0..1 ──creates──▶ LeaveRecord
LeaveRecord.SourceLeaveRequestId  nullable unique
```

- FK direction: `LeaveRecord.SourceLeaveRequestId` → `LeaveRequests.Id` (Restrict)  
- Direct HR `LeaveRecord` (HR-05A): `SourceLeaveRequestId = null`  
- **One** approved request → **one** LeaveRecord (idempotent)  
- No reverse `LeaveRequest.LeaveRecordId` column

Do **not** merge tables. Do **not** put Pending/Approved on `LeaveRecord.Status`.

---

## 7. Status model (MVP)

```text
        submit
Pending ──────► Approved ──(creates LeaveRecord)
   │               │
   │ withdraw      │ cancel-approved (authorized)
   ▼               ▼
Cancelled      Cancelled
                   (+ cancel LeaveRecord via HR-05A cancel)

Pending ──reject──► Rejected
```

| Status | LeaveRecord | Balance effect |
|--------|-------------|----------------|
| Pending | none | none |
| Approved | exactly one Recorded | uses Amount |
| Rejected | none | none |
| Cancelled | none, or Cancelled if was Approved | none after cancel |

**Rules:**

| Question | MVP freeze |
|----------|------------|
| Employee cancel Pending? | **Yes** (withdraw) → Cancelled |
| Employee cancel Approved? | **No** — must request HR/authorized cancel-approved |
| Cancel Approved | Marks request Cancelled + cancels generated LeaveRecord (atomic); histories retained |
| Rejected reopen? | **No** — create new request |
| Edit after submit? | **No** — immutable; withdraw + new request |

No `CancelledAfterApproval` separate status — `Cancelled` + presence of linked cancelled LeaveRecord is enough. Optional decision rows record *why*.

---

## 8. Approval topology (MVP)

### Recommended: **B — Department approver → HR final**

```text
Employee / on-behalf submit
  → Pending (awaiting Department)
  → Department-authorized user with hr.leave.approve
       → PendingHr (or equivalent stage field)
  → HR with hr.leave.manage (or hr.leave.approve at Property-wide)
       → Approved + LeaveRecord
```

**Why B:**

- WebİK portal **CONFIRMED** `asama`: Departman → İK  
- Hotels typically want HR visibility on annual / paid leave  
- AUTH-02 gives Department *where* without inventing reporting line  
- Still not a configurable multi-stage engine — fixed two stages  

### Deferred

| Option | When |
|--------|------|
| **A** Dept-only single stage | If PO wants faster ops and accepts no HR gate |
| **C** Configurable N-stage | Never in 05B |

### Stage representation

Prefer explicit `ApprovalStage` on request:

| Value | Meaning |
|-------|---------|
| `Department` | Awaiting Department-scoped approver |
| `Hr` | Awaiting Property-wide HR |
| `Done` | Terminal (Approved/Rejected/Cancelled) |

Append-only `LeaveRequestDecision` rows for each action.

---

## 9. Permissions (WHAT) vs Department scope (WHERE)

### Proposed permissions

| Code | Scope kind | Semantics |
|------|------------|-----------|
| `hr.leave.read` | Property (existing) | View leave facts / requests in tenant |
| `hr.leave.manage` | Property (existing) | Direct LeaveRecord CRUD (05A); HR approve stage; cancel-approved; type admin |
| `hr.leave.request` | Linked-employee / Property | Create/withdraw own (or on-behalf if also manage) requests |
| `hr.leave.approve` | **Department-aware** | Approve/reject at Department stage within AUTH-02 scopes |

**Mandatory separation:** Department scope without `hr.leave.approve` → cannot approve.

**Implication:**

- `hr.leave.manage` **does** allow HR final approval and cancel-approved (Property-wide), **does not** replace Department stage unless actor also has Property-wide approve policy defined as “skip dept when Property-wide HR” — **recommended:** Property-wide actors with `hr.leave.manage` may approve **both** stages (practical hotel HR).  
- Department managers need `hr.leave.approve` + Department scopes.  
- Employees need account link + `hr.leave.request` (or policy: linked employee may request without catalog permission — **open decision §53**).

Keep HR-05A `hr.leave.read/manage` backward compatible.

---

## 10. Employee self-service identity

```text
ApplicationUser
  → EmployeeAccountLink (UserId → EmployeeId)
  → open/current Employment
  → LeaveRequest
```

| Rule | Detail |
|------|--------|
| Resolve | Link only — **no** email / personnel-number matching |
| No link | Self-service create returns clear error; HR may still create on behalf |
| Employment | Self-service uses current open Employment only |
| Historical | Self-service does not create requests on ended Employments |

**Recommendation:** Domain + API designed for self-service **now** (link foundation exists). First **web UI** may ship management queue + on-behalf create; employee “İzinlerim” can follow in same sprint or immediate follow-up if link UX is production-ready.

---

## 11. RequestedAmount & HR-06 schedule integration

### Principle

| Layer | Role |
|-------|------|
| Schedule (HR-06) | PLAN — consulted for **preview** only |
| LeaveRequest | Intent + frozen FinalAmount at approval |
| LeaveRecord.Amount | **Authoritative** after approval — never silently rewritten by later schedule edits |

### Preview algorithm (deterministic)

For each Property-local date `D` in `[StartDate, EndDate]`:

| Schedule state | Contribution |
|----------------|--------------|
| **Scheduled** | +1.0 to suggested chargeable days |
| **RestDay** | +0.0 (excluded) |
| **Unscheduled** | Mark range **ScheduleIncomplete**; do not invent RestDay |

**SuggestedAmount** = count of Scheduled days (or half-day policy below).

**Example:**

| Day | State | Charge? |
|-----|-------|---------|
| Mon | Scheduled | yes |
| Tue | Scheduled | yes |
| Wed | RestDay | no |
| Thu | Scheduled | yes |
| Fri | Scheduled | yes |
| Sat | RestDay | no |
| Sun | Unscheduled | incomplete flag |

SuggestedAmount = **4.0** with `ScheduleIncomplete = true`.

### Incomplete schedule policy (recommended freeze)

- Create/approve **allowed** with warning when incomplete.  
- Approver **must** explicitly set/confirm `FinalAmount` (cannot blind-approve auto-suggest alone if incomplete).  
- UI shows per-day schedule summary.

**Alternative (stricter):** block submit if any Unscheduled — mark as open decision if PO prefers hard block.

### Half-day

- **MVP:** support `0.5` **Amount quantum only** (same as HR-05A).  
- Same-day request: `RequestedAmount` ∈ {0.5, 1.0}.  
- **No** AM/PM / morning-afternoon schedule semantics.  
- Multi-day: `RequestedAmount` in 0.5 steps; default suggestion = Scheduled-day count (integer); user/approver may adjust to half-day totals.

### Hourly leave

**DEFERRED.** Do not mix hours into day Amount. Separate future unit model if needed.

### Schedule mutation after request

| Event | Behavior |
|-------|----------|
| Schedule changes while Pending | On approve, **re-run preview**; show delta vs RequestedAmount; approver confirms FinalAmount |
| After Approved | LeaveRecord.Amount **immutable** re: schedule; schedule may still change independently |

### Public holidays / weekends

No hardcoded Saturday/Sunday/Turkish holidays. Only explicit RestDay / Scheduled / Unscheduled.

---

## 12. Overlap rules

| Case | Rule |
|------|------|
| Pending ∩ Pending (same Employment) | **Reject** |
| Pending ∩ Approved (active request) | **Reject** |
| Pending ∩ Recorded LeaveRecord | **Reject** (05A overlap) |
| Rejected / Cancelled ∩ new | **Ignored** for overlap |
| Approved request’s LeaveRecord Cancelled | Request Cancelled; new request may cover dates |

“Active request” = Status ∈ {Pending, Approved} with Approved still linked to **Recorded** LeaveRecord.

---

## 13. Balance validation

Preserve HR-05A: derived remaining may go negative.

| Moment | Behavior |
|--------|----------|
| Create | Soft **warning** if TracksBalance and RequestedAmount > remaining |
| Approve | Soft **warning** again; **do not hard-block** by default |

Strict block deferred unless PO mandates.

---

## 14. Decisions / audit

### LeaveRequestDecision (append-only)

| Field | Purpose |
|-------|---------|
| `Id`, `LeaveRequestId` | Identity |
| `Action` | Submit / ApproveDepartment / ApproveHr / Reject / Withdraw / CancelApproved |
| `ActorUserId`, `AtUtc` | Who/when |
| `Note` | Optional |
| `AmountSnapshot` | Optional FinalAmount at approve |

Do **not** overwrite a single DecisionBy column for multi-stage.

---

## 15. Cancellation & rejection

| Action | Effect |
|--------|--------|
| Withdraw (Pending) | Status Cancelled; decision row; no LeaveRecord |
| Reject | Status Rejected; note recommended; no LeaveRecord; immutable |
| CancelApproved | Status Cancelled; cancel linked LeaveRecord via existing cancel path (reason required); one transaction |

Never hard-delete LeaveRequest or LeaveRecord.

---

## 16. Editing

**Submitted requests immutable.** Wrong data → withdraw (Pending) or cancel-approved + new request. No versioning table.

---

## 17. Attachments / substitute

| Feature | Classification |
|---------|----------------|
| Attachments (medical etc.) | **DEFERRED** |
| Substitute employee | **DEFERRED** |
| Notifications | Domain hooks only; infra **DEFERRED** |
| Mobile UI | **DEFERRED** (API consumer-neutral) |

---

## 18. LeaveType lifecycle vs request

| Event | Rule |
|-------|------|
| Inactive type | **Cannot** create new request |
| Type deactivated while Pending | Approver may still **approve or reject** (historical workflow); cannot start new requests of that type |
| Non-TracksBalance types | Request allowed; no balance warning |

---

## 19. Employment boundary

- One LeaveRequest = one Employment.  
- Dates must lie inside Employment period (same rules as LeaveRecord).  
- Self-service: current open Employment only.  
- HR on-behalf: may select Employment explicitly (including ended only if product allows historical — **recommend: Active/Scheduled Employment only for requests**; historical corrections stay direct LeaveRecord).

---

## 20. Transaction / concurrency / idempotency

### Approve (single DB transaction)

1. Load request (Pending + correct stage)  
2. Authorize actor  
3. Revalidate type / employment / overlap / amount  
4. Recompute schedule preview (advisory)  
5. Create LeaveRecord with `SourceLeaveRequestId`  
6. Append decision; set Approved + FinalAmount + LeaveRecordId  
7. Commit  

Failure → request remains Pending; no orphan LeaveRecord.

### Double approve

- Conditional update: `WHERE Status = Pending AND ApprovalStage = …`  
- Unique index on `LeaveRecord.SourceLeaveRequestId` (WHERE NOT NULL)  
- Second approver gets conflict — **no** second LeaveRecord  

No distributed locks.

### CancelApproved

Atomic: cancel LeaveRecord + set request Cancelled + decision row.

---

## 21. API proposal (conventions)

Align with existing `/api/hr/...` + antiforgery on writes + Problem Details `code`.

### Self-service

| Method | Path | Auth |
|--------|------|------|
| GET | `/api/hr/my/leave-overview` | Linked employee |
| GET | `/api/hr/my/leave-requests` | Linked |
| POST | `/api/hr/my/leave-requests` | `hr.leave.request` + link |
| POST | `/api/hr/my/leave-requests/{id}/withdraw` | Owner + Pending |

### Management

| Method | Path | Auth |
|--------|------|------|
| GET | `/api/hr/leave-requests` | read + tenant (+ dept filter for approvers) |
| GET | `/api/hr/leave-requests/{id}` | same |
| POST | `/api/hr/leave-requests` | manage or request-on-behalf |
| POST | `/api/hr/leave-requests/{id}/approve` | approve / manage by stage |
| POST | `/api/hr/leave-requests/{id}/reject` | approve / manage |
| POST | `/api/hr/leave-requests/{id}/cancel-approved` | manage |
| POST | `/api/hr/leave-requests/preview-amount` | same as create |

Exact route names may adjust to match endpoint style; consumer-neutral for future mobile.

Existing HR-05A leave-record endpoints **unchanged**.

---

## 22. Database proposal (no migration now)

### LeaveRequests

| Column | Notes |
|--------|-------|
| Id | PK |
| EmploymentId | FK Restrict |
| AssignmentId | FK Restrict |
| LeaveTypeId | FK Restrict |
| StartDate, EndDate | date |
| RequestedAmount, FinalAmount | numeric(6,1); Final nullable |
| Reason | varchar nullable |
| Status | int/enum |
| ApprovalStage | int/enum |
| SchedulePreviewIncomplete | bool |
| LeaveRecordId | nullable FK |
| Created*/Updated* | audit |

Indexes: `(EmploymentId, Status)`, `(AssignmentId)`, date range queries, `(LeaveRecordId)` unique where not null.

Check: Start ≤ End; Amount quantum; Status/Stage consistency.

### LeaveRequestDecisions

Append-only; FK Cascade or Restrict to request; index `(LeaveRequestId, AtUtc)`.

### LeaveRecords alteration (05B migration later)

| Column | Notes |
|--------|-------|
| `SourceLeaveRequestId` | nullable Guid, **unique** where not null, FK Restrict |

---

## 23. Error codes (proposed)

| Code | HTTP |
|------|------|
| `leave-request-not-found` | 404 |
| `leave-request-link-required` | 400/403 |
| `leave-request-employment-not-active` | 400 |
| `leave-request-date-outside-employment` | 400 |
| `leave-request-type-inactive` | 400 |
| `leave-request-invalid-amount` | 400 |
| `leave-request-overlap` | 409 |
| `leave-request-schedule-incomplete` | 400 (if hard-block) or warning-only extension |
| `leave-request-cross-department-range` | 400 |
| `leave-request-not-pending` | 409 |
| `leave-request-wrong-stage` | 409 |
| `leave-request-already-decided` | 409 |
| `leave-request-unauthorized-department` | 403 |
| `leave-request-record-conflict` | 409 |
| `leave-overlap` | 409 (existing record overlap) |

---

## 24. UI proposal (not implemented)

### Workforce → İzin Yönetimi (management)

Tabs: Bekleyen | Onaylanan | Reddedilen | İptal  
Filters: Department, Employee, LeaveType, dates  
AUTH-02: Department approvers see authorized depts only; Property-wide HR sees Property.

Detail: employee identity, type, dates, Requested/Final amount, balance warning, **per-day schedule summary** (Scheduled / RestDay / Unscheduled), reason, decision history. Actions by stage.

### Employee → İzinlerim (self-service)

Balances; New request (type, dates, preview, amount, reason); lists by status.

### Personnel Card Leave tab

Keep 05A: Bakiyeler | Hakediş | Kayıtlı izinler.  
Add read-only **Talepler** section (list/link). **Do not** turn card into approval workspace.

---

## 25. Security findings

| Threat | Mitigation |
|--------|------------|
| Request for another employee | Link / on-behalf permission only |
| Cross-department peek | AUTH-02 + Assignment Department on request |
| Tampered EmploymentId / LeaveTypeId | Server resolve + org/property guards |
| Direct status mutation | No client status write; transitions only |
| Double approve | Conditional update + unique SourceLeaveRequestId |
| Count leakage | Queries apply same filters as list rows |

Backend authoritative. No frontend-only auth.

---

## 26. Domain boundaries (explicit)

```text
LeaveRequest  ≠  LeaveRecord
LeaveRecord   ≠  ScheduleEntry
RestDay       ≠  Leave
Unscheduled   ≠  RestDay

HR-06 = PLAN
Leave = approved / HR-recorded ABSENCE FACT
HR-07 (future) = ACTUAL attendance
```

Attendance **not** required for leave approval.

---

## 27. Deferred / classification

| Feature | Class |
|---------|-------|
| Hourly leave | DEFERRED |
| Attachments | DEFERRED |
| Substitute employee | DEFERRED |
| Public holiday engine | DEFERRED |
| Notifications infra | DEFERRED (hooks OK) |
| Mobile UI | DEFERRED |
| Manager reporting line | DEFERRED |
| Configurable N-stage workflow | DEFERRED |
| Delegated approver | DEFERRED |
| Leave calendar heatmap | DEFERRED |
| Payroll / payout | DEFERRED |
| Strict balance block | NEEDS VALIDATION (default soft warn) |
| Hard-block Unscheduled | NEEDS VALIDATION (default warn + confirm FinalAmount) |
| Single-stage dept-only (A) | NEEDS VALIDATION vs recommended B |

---

## 28. Test matrix (summary)

Creation (self / on-behalf); link missing; permissions; Department scope; Property-wide HR; schedule Scheduled/RestDay/Unscheduled; half-day; balance warning; overlaps; reject; withdraw; approve dept→HR; LeaveRecord create; atomic rollback; double approve; idempotency; cross-dept range reject; Employment boundary; inactive type; transfer; cross-property/org; concurrency.

---

## 29. Open decisions — Accepted answers (2026-08-31)

| Q | Decision |
|---|----------|
| Q1 Approval stages | **B** Department → HR final |
| Q2 Unscheduled in range | **A** Allow with warning; approver confirms FinalAmount |
| Q3 Self-service permission | **B** Require `EmployeeAccountLink` + `hr.leave.request` |
| Q4 First UI ship order | Deferred to UI slices (domain unchanged) |
| Q5 Balance over-request | **A** Soft warn, allow (matches HR-05A) |

---

## 30. Recommended freeze (one-page)

1. Introduce **LeaveRequest** on **Employment** + `AssignmentId` at StartDate.  
2. Keep **LeaveRecord** as sole balance fact; add nullable unique `SourceLeaveRequestId`.  
3. Statuses: Pending / Approved / Rejected / Cancelled; append-only decisions.  
4. Topology **B**: Department approve → HR final; AUTH-02 WHERE + `hr.leave.approve` WHAT.  
5. Schedule preview: Scheduled chargeable; RestDay exclude; Unscheduled → incomplete warning.  
6. FinalAmount frozen at approval; later schedule changes do not rewrite LeaveRecord.  
7. Cross-department date range → reject create (split requests).  
8. Half-day amount yes; hourly no; soft balance warning; immutable after submit.  
9. Direct HR LeaveRecord remains; EmployeeAccountLink for self-service.  
10. No ScheduleEntry.Kind Leave; no OFF ShiftDefinition; no BPM engine.

---

## Related

- [HR-05A-Leave-Foundation.md](HR-05A-Leave-Foundation.md)  
- [HR-05B-Leave-Request-Approval-Implementation-Plan.md](HR-05B-Leave-Request-Approval-Implementation-Plan.md)  
- [HR-06-Shift-Work-Schedule.md](HR-06-Shift-Work-Schedule.md)  
- [DEPARTMENT_MEMBERSHIP_SCOPE.md](../../security/authorization/DEPARTMENT_MEMBERSHIP_SCOPE.md)

---

## Slice B — API implementation notes (2026-08-31)

**Status:** Accepted (CTO + PO)

### Routes

Self-service (policy `HrLeaveRequest` + `employee_id` claim from EmployeeAccountLink):

- `GET /api/hr/my/leave-requests`
- `GET /api/hr/my/leave-requests/{id}`
- `POST /api/hr/my/leave-requests` (LeaveTypeId, StartDate, EndDate, RequestedAmount, Reason?)
- `POST /api/hr/my/leave-requests/preview`
- `POST /api/hr/my/leave-requests/{id}/withdraw`
- `GET /api/hr/my/leave` (Slice C: active leave types + balances for linked employee; no admin surface)

Management (`HrLeaveRead` / approve / manage as applicable; AUTH-02 on persisted Assignment):

- `GET /api/hr/leave-requests`
- `GET /api/hr/leave-requests/{id}`
- `POST /api/hr/leave-requests/{id}/department-approve`
- `POST /api/hr/leave-requests/{id}/reject`
- `POST /api/hr/leave-requests/{id}/approve` (FinalAmount required)
- `POST /api/hr/leave-requests/{id}/cancel-approved`

### Permissions seeding

- HR system templates: `read` + `manage` + `approve` (not `request`)
- `DepartmentSchedulerPermissions` bundle: schedule + `hr.leave.read` + `hr.leave.approve` (still not a bound SystemRoleTemplate)
- `EmployeeLeaveSelfServicePermissions` / system role `employee-leave-self-service`: `hr.leave.request` only — assigned in development to operational employee personas (e.g. maintenance technician) alongside their primary role; also assignable via role admin

### Schedule preview

Scheduled = 1 day charge candidate; RestDay = 0; Unscheduled = 0 + ScheduleIncomplete. SuggestedAmount = sum of Scheduled only.

---

## Slice C — Management + Self-Service UI (2026-08-31)

**Status:** Accepted / Completed — Product Owner runtime acceptance (2026-08-31)

**Whole HR-05B:** Accepted / Completed. Personnel Card request history remains deferred as a follow-up surface.

### Frontend routes

- `/app/workforce/leave-management` — management workspace (tabs Bekleyen/Onaylanan/Reddedilen/İptal; stage chips; filters; detail + actions)
- `/app/my/leave` — self-service (balances, new request + preview, withdraw Pending)

### UI rules locked in this slice

- FinalAmount default = RequestedAmount (SuggestedAmount advisory)
- Employee RequestedAmount not overwritten after manual edit
- Department approve copy: sends to HR; no LeaveRecord language
- RestDay ≠ Unscheduled; incomplete + negative balance warnings non-blocking
- No EmployeeId / EmploymentId / AssignmentId / FinalAmount on employee create form
- Personnel Card request history: **deferred**
