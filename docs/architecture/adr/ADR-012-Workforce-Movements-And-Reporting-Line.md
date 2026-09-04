# ADR-012: Workforce Movements and Reporting Line

> Copy of the [ADR Template](ADR-TEMPLATE.md) filled for HR-08. **Accepted.**

---

## Status

**Accepted** — Product Owner + CTO (2026-09-04)

This ADR freezes domain direction for **Personel Hareketleri / Workforce Movements**. **HR-08A is Accepted / Completed** (Assignment-compatible movements, `PersonnelMovement`, `WorkforceReportingLine`, APIs, permissions, shared active-Property context). It does **not** authorize the HR-08B React workspace or sidebar. **HR-08 overall remains In Progress** until HR-08B.

Does not supersede HR-DOMAIN-001, HR-04 (manager still unimplemented), HR-05B, HR-06, HR-07, ADR-010, or ADR-011.

---

## Context

HuGu already persists organizational posting as dated **Assignment** rows:

```text
Employee → Employment → Assignment → Department + Position → Property
```

`TransferEmployeeUseCase` closes the previous Primary on **D−1** and inserts a new Primary on **D**. DepartmentId/PositionId on old rows are not overwritten. Puantaj, schedule, and leave-request **create** resolve Primary Assignment **on a local date**.

Gaps:

- Transfer has no semantic type (promotion vs lateral), reason, note, or actor.
- Manager / reporting line is explicitly **deferred** (HR-DOMAIN-001, HR-04 `EmployeeReportingLine` sketch).
- Transfer authorization is destination-Property `workforce.manage` only.
- WebİK shows both a card-level overwrite+`gecmis` log and an Atama & Terfi BPM module. TimeCore “Personel Hareketleri” is **PDKS punches**, not this domain. HuGu must not copy current-state overwrite, title/kademe managers, or destructive Geri Al.

Product freeze requested: **Personel Hareketleri** is a **top-level HR operational module**, not a Personnel Card tab.

Slice id **HR-08** in current planning means Workforce Movements. Older Personel Master text used HR-08 for Attendance; ADR-011 still mentions a later overtime consumer as “HR-08.” Those Accepted documents are not rewritten.

---

## Problem

How should HuGu record department, position, promotion, property, and manager changes so that:

- historical workplace context remains reconstructable for Puantaj / leave / future payroll and performance,
- promotion is distinguishable from a lateral position change,
- manager-on-a-date is a workforce fact (not a user role or job title),
- same-Organization tesis transfer does not clone Employee,
- cross-Organization moves do not silently reuse Employment,
- mistakes are corrected without destroying history,

without introducing a generic “Movement” table that replaces Assignment, and without building an org-chart/BPM engine now?

---

## Decision

We will:

### 1. Treat Personel Hareketleri as a top-level domain/module

UI labels: TR **Personel Hareketleri**, EN **Workforce Movements**, RU **Кадровые перемещения**.

Personnel Card remains master data. Create/manage movements belong on the top-level module. A later read-only card timeline (HR-08B) may deep-link; it is not the write surface.

Do not implement menu/UI in the discovery slice.

### 2. Keep Assignment as structural organizational history

Assignment remains the source of truth for **where** someone worked (Department, Position, and via Department, Property) on a `DateOnly`.

Transfer-style writes stay: close previous Primary on **D−1**, create a new Primary on **D**. Do not mutate old DepartmentId/PositionId. Do not overwrite current FKs on Employee.

Do **not** introduce a generic movement aggregate that *owns* Assignment as a projection (Model 3).

### 3. Add a lightweight PersonnelMovement event (Model 2)

When HR-08 is implemented, persist an immutable business event **in addition to** Assignment/ReportingLine writes:

- `MovementType`, `EffectiveDate`, `EmploymentId`, `OrganizationId`
- optional `PreviousAssignmentId` / `NewAssignmentId`
- optional reporting-line ids for manager movements
- `Reason` / `Note`, `ActorUserId`, `CreatedAtUtc`

This event answers **what kind of change, why, who**. Assignment answers **structural state as-of date**.

Hire and End Employment remain their own commands; they are not required to emit this event in HR-08 MVP.

### 4. Effective-date semantics

Every organizational movement uses Property-local **DateOnly**. Audit timestamps are UTC.

Effective **D**: old Primary covers through **D−1**; new Primary from **D**. No time-of-day split.

### 5. One-primary-assignment overlap invariant

Unchanged from Accepted workforce invariants:

- At most one Primary Assignment covers any date per Employment.
- Primary periods must not overlap.
- Adjacent D−1 / D is required, not a gap and not a shared day.

`AssignmentKind.Temporary` remains unimplemented. HR-08 MVP does not introduce concurrent primaries.

### 6. Same-Organization Property transfer

Ankara → İstanbul **inside one Organization** is an **Assignment** transfer: same Employee, same Employment, new Primary whose Department belongs to the destination Property. Department and Position are reselected (Property-scoped + applicability).

`PersonnelNumber` uniqueness remains Organization-scoped; identity is not cloned.

### 7. Cross-Organization boundary

Cross-Organization is **not** Property Transfer. Do **not** reuse Employment. That is a new membership / new Employment in the other Organization (or future hotel-group product). Rehire after End Employment is a new Employment, not a movement.

### 8. Promotion semantic rule

Promotion is a **semantic** movement type, not a Grade entity, and not automatically every Position change.

MVP: **Terfi** requires a Position change. The structural write is a new Assignment; the event type is `Promotion` so the timeline is not “Pozisyon Değişikliği.” Grade/Kademe stays deferred.

### 9. Manager / reporting-line model

Implement (when authorized) a separate effective-dated **WorkforceReportingLine**:

- `SubordinateEmploymentId`, `ManagerEmploymentId`
- `EffectiveFrom` / `EffectiveTo` (`DateOnly`)
- MVP: one **primary** direct manager at a time (non-overlap, D−1 / D)

Resolve “manager on date D” from ReportingLine covering D.

**Reject:**

- `Employment.ManagerEmploymentId` as the only current FK (no history)
- `Assignment.ManagerAssignmentId` (manager is a person/employment, not a posting; manager’s own transfer would break subordinates)
- `Employee.ManagerEmployeeId` (wrong grain; current-state; rehire leakage)
- `ApplicationUser` manager relationship
- Inference from Position title, kademe, or names

Department/Property movements do **not** silently rewrite reporting line; the flow may record a Manager Change in the **same transaction**.

Self-manager and as-of cycles are forbidden.

### 10. Reporting-line temporal semantics

Same DateOnly rule as Assignment. Future-dated manager changes are allowed if movements are. Matrix / org-chart / department-head inference are deferred.

### 11. Correction / reversal

- **Not yet effective:** cancel; do not leave overlapping or orphan primaries; no generic hard delete of the business fact after it would have applied.
- **Already effective:** new reversal/correction movement; do not mutate historical Assignment org FKs; do not delete history rows (WebİK Geri Al is rejected).

### 12. Authorization scope

Catalog (do not seed in this discovery): `hr.movements.read`, `hr.movements.manage`; future `hr.movements.approve` unused in MVP.

Same-Property structural movements: property-wide HR manage; not department-scheduler.

**Property Transfer:** actor must manage **both source and destination Properties**, or hold `hr.movements.manage` on an **Organization-wide** membership. Destination-only (today’s Transfer hole) is **not** the product rule.

No role-name checks.

### 13. Görev Değişikliği

Not a distinct MVP movement type until product meaning is frozen. Do not add a generic duty string. Do not confuse with `EmploymentDutyCode` or ApplicationUser roles.

---

## Alternatives Considered

| Alternative | Outcome | Reason |
|-------------|---------|--------|
| Assignment history only (Model 1) | **Rejected** as the full product model | Cannot distinguish Terfi vs Position Change; no reason/actor; manager is not an Assignment |
| Generic movement aggregate owns Assignment (Model 3) | **Rejected** | Two sources of truth; Puantaj already depends on Assignment; over-abstracted |
| Assignment + PersonnelMovement (Model 2) | **Accepted** | Structural SoT unchanged; semantics/audit for the timeline |
| WebİK kart overwrite + `gecmis` JSON | **Rejected** | Current-state SoT; destructive Geri Al; string org |
| WebİK Atama & Terfi BPM in MVP | **Deferred** | Useful later; not required to freeze history |
| `Employment.ManagerEmploymentId` only | **Rejected** | Overwrites history |
| `Assignment.ManagerAssignmentId` | **Rejected** | Couples manager to posting; manager transfer churn |
| `Employee.ManagerEmployeeId` | **Rejected** | Current-state; wrong lifetime vs Employment |
| Effective-dated ReportingLine (Employment–Employment) | **Accepted** | Dated, survives Assignment change, rehire-safe |
| Title/kademe manager (WebİK) | **Rejected** | Violates Employee ≠ Role ≠ Position |
| Cross-org reuse of Employment | **Rejected** | Wrong employer boundary |
| Property transfer as End+Rehire inside one Organization | **Rejected** | Breaks seniority/identity; Assignment already spans Properties |
| Grade domain for Promotion | **Rejected for MVP** | Speculative; HR-01 deferred Grade |
| Bulk reorganization in MVP | **Deferred** | Not trivial under applicability + dual-Property auth |
| Change Transfer production behavior in this ADR slice | **Rejected** | Discovery only |

---

## Consequences

### Positive

- Puantaj/schedule dated Assignment resolution remains valid.
- Timeline can show Terfi vs Pozisyon Değişikliği vs Tesis transfer vs Yönetici değişikliği.
- Manager-on-date is available for future leave routing and performance without ApplicationUser.
- Same-Organization multi-property identity stays one Employee.
- Audit matches foundation pattern B (domain-owned, not a generic JSON dump).

### Negative

- Two writes per structural movement (Assignment pair + event) — must stay one transaction.
- Existing Transfer rows have **no** movement events (no backfill in MVP unless later approved).
- Dual-Property authorization is stricter than today’s Transfer.
- Pending LeaveRequest vs later Transfer remains a known Leave-domain risk until an implementation guard is added.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Operators treat Assignment as “current only” and demand in-place edits | UX: previous vs new; card org fields stay read-only |
| Naming clash with WebİK PDKS Personel Hareketleri | Copy in product docs; EN type name `PersonnelMovement` / Workforce Movements |
| Slice-id clash with older HR-08 Attendance / overtime | Planning index note; do not rewrite Accepted texts |
| ReportingLine cycles | Domain validation on as-of graph |
| Destination-only Transfer left in production until UI slice | Implementation slice must replace/wrap Transfer with dual-Property rule |
| Over-building org chart | MVP one primary manager; no matrix |

---

## Revisit Conditions

- PO rejects Q1–Q10 in [HR-08 discovery](../../product/hr/HR-08-PERSONEL-HAREKETLERI-DISCOVERY.md).
- Hotel evidence requires concurrent Temporary assignments or matrix reporting.
- Payroll/legal documents require frozen movement snapshots beyond Assignment ids.
- Hotel Group / multi-Organization employment is accepted.
- Grade/Kademe becomes a real product.

---

## Deferred items

HR-08A implemented foundation entities, APIs, and permissions. Still deferred (HR-08B and later):

- Top-level Personel Hareketleri sidebar, list UI, Yeni Hareket workflow, movement detail UI
- Personnel Card read-only movement history
- Rename legacy Personnel Card “Görev değişikliği” UI copy
- `hr.movements.approve` and kurul-style workflow
- Görev Değişikliği as a distinct movement type
- Grade / Kademe / Salary movement
- Temporary Assignment UI
- Bulk movements
- Movement documents
- Org chart
- Cross-Organization transfer
- Rehire UI
- Same-day hire-date Transfer exception
- Wiring leave approval to ReportingLine (HR-05B remains department + HR)

---

## Date

2026-09-04 proposed; **Accepted 2026-09-04**.

---

## HR-08A implementation freeze

**Status: Accepted / Completed** (live PO/CTO acceptance 2026-09-04).

- **Assignment** remains structural source of truth. `PersonnelMovement` is a semantic/audit event and does not own Department/Position/Property.
- **WorkforceReportingLine** is the effective-dated manager relationship (Employment-to-Employment). Manager is never `ApplicationUser` and is never inferred from title/Position. One covering direct manager per date.
- Promotion requires a Position change and is never inferred from Transfer or PropertyTransfer.
- Same-Organization PropertyTransfer only. Cross-Organization is a separate Employment lifecycle, not this type.
- **Lifecycle is derived**, not stored: `Cancelled` if `CancelledAtUtc` is set; else `Effective` if `EffectiveDate <=` Property-local today (`Property.TimeZoneId` + `TimeProvider`/`IWorkforceClock.UtcNow`); else `Scheduled`.
- Future assignment rows are allowed. Current-assignment resolvers use **Covering(date)**, never latest-row. Directory/tenant/SGK property fallbacks that picked a future Primary were removed.
- Cancel of a not-yet-effective assignment movement **reopens** the previous Assignment, **deletes** the never-effective successor Assignment, **nulls** `NewAssignmentId` (FK integrity), and **keeps** the `PersonnelMovement` row as cancelled. Effective movements are not cancelled or hard-deleted.
- Types: `DepartmentChange`, `PositionChange`, `Promotion`, `PropertyTransfer`, `ManagerChange`. Legacy `POST /api/workforce/employees/{id}/transfer` maps department-only → `DepartmentChange`, position-only → `PositionChange`, both in the same property → `AssignmentChange` (not Promotion). Cross-property legacy Transfer → `PropertyTransfer`. Public `POST /api/hr/movements` does not accept `AssignmentChange`.
- Pending `LeaveRequest` whose range **splits** D (`Start < D && End >= D`) **blocks** assignment movements. Recorded `LeaveRecord` does not. `ManagerChange` is not blocked by leave.
- Future `ScheduleEntry` or `AttendanceCorrection` on/after D still pinned to the old Assignment **blocks** the movement (no automatic migration).
- Reporting line is Employment-to-Employment, same Organization, **cross-Property allowed**, one covering primary manager per date, 2- and N-node cycle rejection.
- Permissions: `hr.movements.read` / `hr.movements.manage` granted to HR Manager/Specialist/Corporate templates. `hr.movements.approve` catalogued, not granted. Department scheduler is not granted manage.
- PropertyTransfer authorization: source **and** destination in the caller’s accessible properties, or organization-wide membership (`PropertyId` null). Dual-Property rule is not weakened for corporate HR.
- List visibility: a movement is visible if any of its source/destination Assignment properties (or covering Assignment property for ManagerChange) overlaps the caller’s accessible set.
- Shared active Property: `ActivePropertyCookie.Bind` + `ActiveWorkplaceResolution` so `/me` and `IWorkplaceContext` agree on the same request as `PUT /api/auth/property` / `RefreshSignIn`. Property-scoped membership cannot escalate via cookie. Org-wide users may select an authorized Property. Unauthorized selection is rejected. **No** first/default Property fallback. This lives in request context, not in HR/movement use cases.
- Migration: `20260903214422_AddWorkforceMovementsAndReportingLinesHr08A` (tables `PersonnelMovements`, `WorkforceReportingLines`). Exactly one HR-08A migration.
- HR-08B (sidebar, list UI, wizard, card history) remains deferred and unimplemented. HR-08 overall stays **In Progress** until HR-08B.

---

## Related Documents

- [HR-08-PERSONEL-HAREKETLERI-DISCOVERY.md](../../product/hr/HR-08-PERSONEL-HAREKETLERI-DISCOVERY.md)
- [WORKFORCE_MODEL.md](../../domain/hr/WORKFORCE_MODEL.md)
- [INVARIANTS.md](../../domain/hr/INVARIANTS.md)
- [HR-04-Employment-Working-Conditions.md](../../product/hr/HR-04-Employment-Working-Conditions.md)
- [HR-07-PUANTAJ-DISCOVERY.md](../../product/hr/HR-07-PUANTAJ-DISCOVERY.md)
- [ADR-011-Puantaj-Domain-Model.md](ADR-011-Puantaj-Domain-Model.md)
- [ADR-008-Authorization-Strategy.md](ADR-008-Authorization-Strategy.md)
- [ADR-010-Database-Managed-Authorization.md](ADR-010-Database-Managed-Authorization.md)
- [AUDIT.md](../foundation/AUDIT.md)
- [TIME_AND_TIMEZONE.md](../foundation/TIME_AND_TIMEZONE.md)
- [DEPARTMENT_MEMBERSHIP_SCOPE.md](../../security/authorization/DEPARTMENT_MEMBERSHIP_SCOPE.md)
