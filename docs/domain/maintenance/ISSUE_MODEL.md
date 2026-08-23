# Arıza model — MaintenanceIssue

> **Status:** Accepted — Product Owner + CTO approved reference baseline. Sprint 0.11A. Product: **Arıza**. Technical aggregate: `MaintenanceIssue`.

## 1. Issue vs Work Order

Hotel language uses both **Arıza** (“what is wrong”) and **İş Emri** (“the work to fix it”).

| Option | Shape | Assessment |
|--------|-------|------------|
| **A** | One `MaintenanceIssue` aggregate holds report, assignment, work lifecycle, resolution | Smallest honest model for current workflow |
| **B** | `MaintenanceIssue` + `MaintenanceWorkOrder` | Correct for CMMS (multiple visits, vendors, parts per defect). No evidence the first slice needs two aggregates |
| **C** | Status-only flag on `Room` | Cannot represent multiple faults, assignment, or non-blocking issues |

**Accepted: Option A.**

Current expert workflow is one operational object: Ön Büro / Kat Hizmetleri records a problem; Teknik Servis is assigned; they intervene; they finish or cannot. Phone coordination is that object. HuGuWeb should record **that**, not introduce a second aggregate “because CMMS does.”

`HousekeepingWorkItem` is separate from `RoomReadiness` because hazırlık state can exist without an assignee, and extra guest requests often do not change Dirty→Ready. Technical work **is** the Arıza. Assignment is data on the issue, not a second root.

**İş Emri** may still appear on the UI as a label for the same record. Do not translate the type name. Do not persist `MaintenanceWorkOrder` in 0.11B.

**Revisit:** split only when a real implemented need appears (return visit vs contractor vs internal follow-up as distinct work on one defect).

**Evidence:** EXPERT-SUPPLIED WORKFLOW (one coordination object); REFERENCE MODEL (defer WO split).

---

## 2. Lifecycle

Avoid a six-state explosion. **Assigned is data, not a state.** Do **not** add `Closed`.

| Technical | Turkish direction | Meaning |
|-----------|-------------------|---------|
| `Open` | Açık | Reported. May or may not have an assignee. |
| `InProgress` | Müdahale sürüyor | Work has started. Requires an assigned Employee. |
| `Resolved` | Çözüldü | Fault fixed. Terminal for this issue. |
| `UnableToResolve` | Çözülemedi | Teknik Servis cannot finish now. **Not** Resolved and **not** terminal. If the issue blocks the oda, unavailability **continues**. |

Accepted paths:

```text
Open ──► InProgress ──► Resolved

Open ──► InProgress ──► UnableToResolve ──► InProgress ──► Resolved
```

| Question | First-slice answer |
|----------|-------------------|
| Is Assigned a state? | **No.** `AssignedEmployeeId` is optional on `Open`. |
| Does `InProgress` require assignment? | **Yes.** `AssignedEmployeeId` is required. |
| Is Closed different from Resolved? | **Not in 0.11B.** Do not add `Closed`. `Resolved` is terminal. |
| Does UnableToResolve remain “open”? | **Operationally yes** for FO/serviceability if blocking. Technically it is a distinct status, not `Open`. It is **not** terminal. |
| Can it reopen after Resolved? | **No** in 0.11B. Recurrence is a **new** Arıza. |
| Resume from UnableToResolve? | **Yes** — same Arıza returns to `InProgress`. |

Unassigned `InProgress` is invalid.

**Evidence:** EXPERT-SUPPLIED WORKFLOW (report / work / cannot-fix / inform FO); REFERENCE MODEL (minimal states). Closed by Product Owner + CTO.

---

## 3. Category

Do **not** freeze `AirConditioning | Plumbing | Painting | Electrical` as a C# trade enum. Hotels organize Teknik Servis differently; names are **data**.

**First slice:** property-scoped reference `MaintenanceIssueCategory` (id, name, active). Flat list. No hierarchy / tree.

Customer configurable later. Development seed **illustrative** names as data (not product constants), for example: Klima, Tesisat, Elektrik, Boya, Mobilya / fixture, Oda ekipmanı, Diğer.

Issue **requires** a category. Hotels rename/deactivate; historical issues keep the category id.

**Evidence:** EXPERT-SUPPLIED WORKFLOW (example list, not frozen); REFERENCE MODEL (customer-defined data, same pattern as Department/Position).

---

## 4. Severity vs Priority

| Concept | Meaning | First slice |
|---------|---------|-------------|
| **Severity** | How serious the fault is (engineering) | **Do not persist.** Deferred. |
| **Priority** | How urgently to act (operations) | **Yes.** Accept only `Normal` / `High` / `Urgent` |

Cosmetic paint can be low priority. Occupied-room AC failure is high priority. A minor fault on a VIP arrival oda can be **Urgent** without being structurally severe.

Priority is domain data, auditable, not a scoring engine.

**Evidence:** EXPERT-SUPPLIED WORKFLOW (urgency from occupancy/VIP/blocking); REFERENCE MODEL (do not invent severity scores).

---

## 5. Source / reported by

An Arıza may originate from Ön Büro, Kat Hizmetleri, Teknik Servis, misafir via another desk, inspection, or a common-area observation.

**Do not** enum source departments (`FrontOffice`, `Housekeeping`, …). Department names are customer data and are not authorization.

First slice:

| Field | Role |
|-------|------|
| `ReportedByEmployeeId` | Optional Workforce `Employee` who reported or was recorded as reporter. **Not** `ApplicationUser`. |
| `OriginNote` | Optional free text (“misafir şikayeti”, “kat görevlisi tespit etti”) |
| History `ActingUserId` | Who performed the command in the app (Identity). Actor may be `ApplicationUser` for now because Employee ↔ User linking **does not exist** yet. |

Guest is not an Employee. Guest reports through a department; that is origin note + FO Employee when known.

**Evidence:** ACCEPTED PRODUCT CONTEXT (Employee ≠ User); REFERENCE MODEL (no department enum).

---

## 6. Room vs common area

Teknik Servis eventually works on koridor, lobi, restoran, mutfak, havuz, ekipman. That is not 0.11B.

**Accepted:** first slice is **oda-only**. `MaintenanceIssue` **requires** `RoomId`. Do **not** create `Location`, `Area`, `Asset`, or `Equipment` abstractions in 0.11B.

This does not trap the domain if:

- non-blocking issues are allowed (so “issue” ≠ “room down”)
- serviceability is derived only from **blocking room issues**
- common-area work later adds a location concept without rewriting Arıza into RoomReadiness

Do not invent public-area “rooms.”

**Evidence:** REFERENCE MODEL. Closed by Product Owner + CTO.

---

## 7. Assets

Do **not** build an equipment registry.

First implementation: `RoomId` + description (and category). No `AssetId`.

Future optional `AssetId` (klima, asansör, TV, minibar fridge, pompa) must not be required to close an Arıza today.

No PM, spare parts, vendors, QR, IoT, cost.

**Evidence:** REFERENCE MODEL (compatibility without CMMS).

---

## 8. Assignment and Workforce

Reuse `Employee`. Do **not** duplicate Employee, Department, Position, Employment, Assignment.

- Assign `AssignedEmployeeId` (optional until assigned)
- Assignee must have **Active** employment (same operational bar as housekeeping assignment)
- Position titles (`Teknik Servis Uzmanı`, `Elektrikçi`, `Tesisatçı`) **never** grant permission and **must not** be matched in code
- Do **not** create technical-job enums

**Limitation (document, do not fake):** first slice may assign **any Active Employee** because safe technician eligibility does not yet exist. A later UI may *filter* Active employees by a customer-defined Department id as convenience — that is not authorization.

No team aggregate. “Sorumlu ekip” in hotel talk maps to an Employee (and later a department filter), not a new Team entity.

**Evidence:** ACCEPTED PRODUCT CONTEXT (Workforce + ADR-008). Closed by Product Owner + CTO.

---

## 9. Resolution / outcome

When work ends:

| Outcome | Status | Note | Photo | TemporaryFix |
|---------|--------|------|-------|----------------|
| Çözüldü | `Resolved` | **Required** | Not required | **Out** (no evidence) |
| Çözülemedi | `UnableToResolve` | **Required** (Ön Büro needs why) | Not required | — |

On successful `Resolved`, the resolver **must** declare preparation impact:

| `PreparationImpact` | Meaning |
|---------------------|---------|
| `None` | Tamir oda hazırlığını etkilemedi. Do not reset RoomReadiness. Room Operations unchanged. |
| `RequiresPreparation` | Room Operations must ensure room preparation is required. TS does **not** set Dirty / Clean / Inspected / Ready. |

Who declares impact in 0.11B: the **resolving Technical Service operator**. Future Supervisor review/override may be added later if evidence requires it.

**Evidence:** EXPERT-SUPPLIED WORKFLOW (inform FO if cannot fix; repair ≠ Ready); ACCEPTED PRODUCT CONTEXT (Room Ops S4). Closed by Product Owner + CTO.
