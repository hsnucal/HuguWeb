# HR-08 — Personel Hareketleri Discovery

> **Status:** **Accepted** — Product Owner + CTO (2026-09-04). Q1–Q10 frozen below. **HR-08A:** **Completed**. **HR-08B:** **Accepted / Completed**. **HR-08 overall:** **Completed**.
>
> This document does **not** supersede HR-DOMAIN-001, HR-DOMAIN-002, HR-DOMAIN-003, HR-04, HR-05A, HR-05B, HR-06, HR-07, or AUTH-02. Those remain **Accepted**.
>
> **WebİK** remains a capability reference only. It is not HuGu domain truth, architecture, naming, schema, or UI to copy. Snapshot files are **not** in this repository and must not be committed.
>
> Companion ADR: [ADR-012-Workforce-Movements-And-Reporting-Line.md](../../architecture/adr/ADR-012-Workforce-Movements-And-Reporting-Line.md) — **Accepted**.

---

## Slice identity

| | |
|--|--|
| Slice id | **HR-08** |
| TR | Personel Hareketleri |
| EN | Workforce Movements |
| RU | Кадровые перемещения |

Do **not** rename because older Personel Master planning used **HR-08** for Attendance / puantaj, and ADR-011 still refers to a later overtime consumer as “HR-08.” Those Accepted texts are **not rewritten**. Current planning map: [README.md](README.md).

This module is **not** a Personnel Card tab. It is a **top-level HR operational module**.

Do **not** confuse HuGu **Personel Hareketleri** with WebİK TimeCore **Personel Hareketleri**, which is a PDKS punch / giriş-çıkış ledger (`gGiris`, `gCikis`, `shiftKod`). Different product. Same Turkish phrase.

---

## 1. Product goal

Personel Hareketleri must answer, for any employee on any Property-local date:

- Where did the employee work?
- In which department?
- In which position?
- At which Property (tesis)?
- Who was the manager?
- What changed?
- When did it become effective?
- Who performed the change?
- Why was it changed?
- What was the previous state?
- What became the new state?

History must be reliable enough for:

- Puantaj historical authorization / workplace context
- Leave
- Performance (future)
- Payroll (future)
- Reporting
- Audit
- future legal / personnel documents

**Core principle:** do **not** merely overwrite current employee fields. Historical organizational state must remain reconstructable.

Existing chain (Accepted):

```text
Employee
  └── Employment
        └── Assignment → Department + Position → Property
```

This discovery asks whether Assignment already provides sufficient **structural** temporal history, and what else is required for **semantic** movements and **reporting line**.

---

## 2. Why top-level module

Organizational change is an operational HR control used across the property, often planned, reviewed, and later audited. It is not a personnel-file footnote.

| Surface | Question it answers |
|---------|---------------------|
| Personel / Personnel Card | Who is this person? Current employment and current assignment |
| Çalışma Bilgileri → Organizasyon | Current Property / Department / Position; composes today’s Transfer command |
| Vardiya Planlama / Puantaj / İzin | Consume **dated** Assignment; they must not own movement |
| **Personel Hareketleri** | What organizational change happened, why, by whom, effective when, previous → new |

Personnel Card may later show a **read-only** movement timeline (HR-08B candidate). Create / manage belongs on the top-level module.

Approved primary sidebar **direction** (do **not** implement in this discovery):

```text
Ana Sayfa
Personel
Personel Hareketleri
Puantaj
Vardiya Planlama
İzin Yönetimi
...
```

---

## 3. Existing HuGu Transfer behavior

Inspected: `TransferEmployeeUseCase`, `TransferPlanner`, `Assignment`, `PrimaryAssignments`, `AssignmentDestination`, `DepartmentPositionApplicability`, `HireEmployeeUseCase`, `EndEmploymentUseCase`, `EmployeeHistoryQuery`, Personnel Card transfer form, `POST /api/workforce/employees/{id}/transfer`, tests in `TransferEmployeeTests` / `DepartmentPositionApplicabilityTests` / `ScheduleEntryApplicationTests`.

Endpoint: `POST /api/workforce/employees/{id}/transfer`  
Permission: `workforce.manage` (`AuthorizationPolicies.WorkforceManage`)  
Command: `EmployeeId`, `DepartmentId`, `PositionId`, `EffectiveDate`  
**No** reason, note, actor, movement type, Property id, or manager.

### Exact flow (CONFIRMED)

1. `WorkplaceGuard` requires explicit active Property.
2. Employee must exist in the **Organization**.
3. Destination **Department** and **Position** must exist and belong to the **active Property** (otherwise `department-not-found` / `position-not-found`).
4. `DepartmentPositionApplicability` is checked (`IsPositionApplicableToDepartmentAsync`).
5. Current non-ended Employment is resolved (`CurrentEmployment.Find`). Ended employment → `no-current-employment`.
6. `TransferPlanner.Plan`:
   - employment not ended
   - destination Department/Position **active**
   - applicability true
   - new period starts on `EffectiveDate` and fits Employment
   - exactly one Primary overlaps the open-ended new period
   - destination is not identical Department **and** Position (`same-assignment`)
   - `previousEnd = EffectiveDate.AddDays(-1)` must be `>=` current Primary `StartDate` (same-day transfer on the assignment start date → `invalid-transfer-date`)
7. Current Primary `TryCloseOn(previousEnd)` — mutates **only** `EndDate`.
8. `Assignment.StartPrimary` creates a **new** row (new id, same Employment, new DepartmentId/PositionId, `StartDate = EffectiveDate`, `EndDate = null`, `Kind = Primary`).
9. Overlap check on the planned set; then `AddAssignment` + one `SaveChangesAsync`.

Date rule (CONFIRMED, Accepted invariant 15):

```text
Transfer effective D
  previous Primary ends D−1
  new Primary starts D
```

`DateOnly`. No time-of-day boundary.

### A–P answers

| # | Question | Classification | Answer |
|---|----------|----------------|--------|
| A | What entity changes? | **CONFIRMED** | Current `Assignment.EndDate` is closed; a **new** `Assignment` is inserted. Employment is unchanged. Employee is unchanged. Department/Position master data is unchanged. |
| B | Is old Assignment closed? | **CONFIRMED** | Yes, `TryCloseOn(D−1)`. Row retained. |
| C | Is a new Assignment created? | **CONFIRMED** | Yes, `Assignment.StartPrimary`. |
| D | Is effective date modeled? | **CONFIRMED** | `DateOnly EffectiveDate` on the command; becomes new `StartDate`. |
| E | Does Property transfer work? | **CONFIRMED** | Yes, **if** the actor’s workplace is the **destination** Property and destination Department/Position belong to that Property. Tests: `Upsert_AfterCrossPropertyTransfer_UsesAssignmentPropertyForDate`. Property is **not** a field on Assignment; it is `Assignment → Department.PropertyId`. |
| F | Is Department transfer supported? | **CONFIRMED** | Yes. Same Position may be kept when applicable in the new Department (`Transfer_SamePositionToDifferentDepartment_KeepsPositionAndHistory`). |
| G | Is Position change supported independently? | **CONFIRMED** | Yes. Command always sends both ids; they may differ only in Position. Applicability still required. |
| H | Can Department+Position change together? | **CONFIRMED** | Yes. That is the normal Transfer input. Atomic in one transaction. |
| I | Is historical lookup reliable? | **CONFIRMED** | `PrimaryAssignments.Covering` / `EffectiveAssignmentResolver.ResolvePrimaryAssignmentOnDate`. No current/last/first fallback. Puantaj, schedule, and leave-request create all use this family. |
| J | Does current implementation mutate old rows? | **CONFIRMED** | **Only `EndDate`.** `DepartmentId` / `PositionId` have **no mutators** after create (HR-06 already recorded this). Not a current-state overwrite of org FKs. |
| K | Is there an audit record? | **CONFIRMED** | **No** domain audit for Transfer. EF shadow `CreatedAtUtc` on Assignment is a technical timestamp, not actor/reason/type. Foundation Audit pattern B is **not** applied to Transfer. |
| L | Does transfer preserve old Position/Department? | **CONFIRMED** | Yes, on the closed Assignment row. History query returns both primaries. |
| M | What validations exist? | **CONFIRMED** | Active dest Department/Position; applicability; current employment; assignment fits employment; not same dept+pos; D−1 ≥ current start; primary overlap; dest Property = active Property. |
| N | Is DepartmentPositionApplicability validated? | **CONFIRMED** | Yes. Invalid → `position-not-available-for-department`. Shared Position across departments allowed. |
| O | Does current Transfer support future-dated changes? | **CONFIRMED** | **Yes, structurally.** Planner does **not** compare `EffectiveDate` to `clock.Today`. A future D closes current Primary on D−1 (which may be in the future) and inserts a successor starting D. UI defaults to earliest valid date (day after current start, or today if later). **No** cancel-before-effective API. |
| P | How does Puantaj resolve assignment after transfer? | **CONFIRMED** | Per local date: Employment covering date → `EffectiveAssignmentResolver` → Department → Property. Days before D use old Assignment; D onward use new. Out-of-scope if Department.PropertyId ≠ planning Property. |

### Current Transfer limitations

1. **Semantic type is missing.** Department-only, Position-only, both, and cross-Property are the same command. Promotion cannot be distinguished.
2. **No reason, note, or actor.** Timeline cannot answer “who / why.”
3. **No user-facing movement list.** History lives on Personnel Card Geçmiş as Assignment periods, not as movements.
4. **UX term collision.** Sprint 0.8 labeled Transfer **Görev değişikliği**. That is not a frozen domain type (see §11).
5. **Authorization is coarse.** `workforce.manage` + destination active Property. **No** source-Property check. **Not** department-aware (AUTH-02). A destination-Property HR user can close an Assignment that still belongs to another Property.
6. **No movement documents, approval, bulk, or reversal.**
7. **Same-day correction on hire date is blocked** (Accepted D−1 rule). Sprint 0.8 recorded this as a future domain question.
8. **Temporary `AssignmentKind` is unused.** Enum exists; Hire/Transfer only create Primary.
9. **No reporting line.** Manager is deferred in HR-DOMAIN-001 / HR-04.

---

## 4. Existing Assignment model

### Classification

Assignment is **start/end dated Primary history**, not current-state-only, and **not** a fully immutable event row.

| Aspect | Finding |
|--------|---------|
| Effective-dated? | **Partially.** Open-ended current row (`EndDate = null`) plus closed predecessors. Not a separate “as-of” table. |
| Start/end dated? | **Yes.** `StartDate` required; `EndDate` optional. `DatePeriod.Contains` / `Overlaps`. DB check `CK_Assignments_Period`. |
| Current-state only? | **No.** Transfer retains previous rows. |
| Immutable history row? | **Partial.** Org FKs are immutable after create. `EndDate` is mutated once on close. No actor/reason/type on the row. |
| Property ownership? | **Indirect.** `Department.PropertyId` (immutable after Department create). Assignment has no `PropertyId`. |
| Department required? | **Yes.** |
| Position required? | **Yes.** |
| AssignmentType / Kind? | `Primary` (used) / `Temporary` (conceptual only; no API/UI). |

### Confirmed invariants

From [INVARIANTS.md](../../domain/hr/INVARIANTS.md), [WORKFORCE_MODEL.md](../../domain/hr/WORKFORCE_MODEL.md), and code:

1. **At most one non-ended Employment** per Employee (`Scheduled` or `Active`).
2. **Primary Assignments cannot overlap** (`PrimaryAssignments.HasOverlap`).
3. **At most one non-ended Primary** per Employment in the intended model (open `EndDate = null`). Transfer always closes the overlapping Primary before opening the next.
4. Primary period must **fit Employment** period.
5. New Assignment cannot target inactive Department or Position.
6. Historical Assignment rows are **retained** (no delete, no overwrite of Department/Position).
7. Department and Position are **Property-scoped** and independently referenced; join is `DepartmentPositionApplicability`.
8. `PersonnelNumber` unique within **Organization**, including former staff; not reused.

### Overlap invariant (verify vs assumption)

**Assumption was correct. CONFIRMED in code.**

One Employment may **not** have two simultaneous Primary Assignments. Transfer on D: previous ends D−1, new starts D. Adjacent days are allowed; overlapping days are not.

`Covering(date)` if two Primaries ever overlapped would pick the latest `StartDate` — defensive, not a license to overlap.

**Temporary** assignments are **not** an implemented second concurrent posting. Do not treat `AssignmentKind.Temporary` as available for HR-08 MVP.

---

## 5. WebİK confirmed findings

Reference snapshot: `C:\Users\hsnuc\Desktop\ik.webik.com.tr` (download meta `2026-08-24`, same snapshot as HR-07). Analysis of `index.html`, `pdks_app.v_10969a00.js`, `sync.v_248e7f94.js` only. **No snapshot files were modified or imported.** Frontend store/DTO names are **UI evidence**, not a server schema to copy.

### Naming collision (CONFIRMED)

WebİK **Personel Hareketleri** (`PersonelHareketleri`, `GET /api/hareketler`) is the TimeCore **punch / giriş-çıkış** list (today’s window by default). HuGu must **not** reuse that meaning.

WebİK organizational movement lives in **two other places**:

1. **Personel Kartı save modal** — after editing tracked fields, “Ne değişti?”
2. **Atama & Terfi** module (`atama_terfi`) — dedicated workflow list + `/api/portal/admin/hareket*`

### A–M answers

| # | Question | Classification | Answer |
|---|----------|----------------|--------|
| A | Dedicated personnel movements module? | **CONFIRMED** | **Atama & Terfi** is a first-class module (sibling of Personel, Disiplin, Zimmet). Kart **Geçmiş** is a second, card-local timeline. TimeCore “Personel Hareketleri” is **not** this. |
| B | Department transfer? | **CONFIRMED** | Kart tracks `departman` with category `departman` (“Departman değişikliği”). Atama & Terfi type `nakil` = “Departman veya firma/şube değişikliği.” Turnover treats department transfer as **not** an exit. Historical department-at-date is reconstructed from `p.gecmis` where `alan === "departman"`. |
| C | Position change? | **CONFIRMED** | Kart field `gorev` (“Görev”); category `gorev` (“Görev değişikliği”). Atama type `gorev` = “Yatay görev değişimi (terfi değil).” Changing department **clears** görev/kademe in the card (applicability by name). |
| D | Firm/property change? | **CONFIRMED** | **Şirket Transferi**: source firma record set `Ayrıldı` + new personel id in target firma; SGK exit code 16 copy; kıdem/izin seed; **does not** submit SGK. Atama type `nakil` can also change `firma`. Nakil is visible in **both** source and destination şube filters (comment: intentional). |
| E | Promotion? | **CONFIRMED** | Atama type `terfi` = “Görev yükselmesi (genelde ücretle birlikte).” Separate from yatay `gorev`. Kurul role “Terfi Kurulu” decides; cannot edit personnel master. |
| F | Manager relation? | **CONFIRMED (weak / title-based)** | **No** person-to-person `yoneticiId` found on the personel record. Org chart uses **görev.bagliGorev** (job-title reporting) plus **kademe**: first 6 kademe = “yönetici” (`YONETICI_KADEME_SAYISI = 6`, Şef included). Leave department filter is `kullanici.departmanlar`, not amir hierarchy (HR-05B already: manager hierarchy **NOT CONFIRMED** for leave). |
| G | Movement effective dates? | **CONFIRMED** | Kart: `gecerlilikTarihi` (defaults today). Same as işe giriş → **no** history row (“hatalı giriş”). Atama: `yururluk` required; comment: approval date ≠ effective date; past or future allowed; apply when `durum='onayli' AND yururluk <= bugün`. |
| H | Historical timeline? | **CONFIRMED** | Kart tab **Geçmiş**: `gecmis[]` entries with kayitTarihi, gecerlilikTarihi, neden, kategori, degisiklikler (eski→yeni), kullanici. Admin **Geri Al** restores old field values and **deletes** that history row. |
| I | Audit? | **CONFIRMED** | `pdksAudit` on card save even for “düzeltme” (history skipped). Separate from `gecmis`. |
| J | Transfer documents? | **NOT CONFIRMED** | No personnel-movement PDF/print flow found in the inspected bundle. Zimmet is a different module (assets). |
| K | Approval workflow? | **CONFIRMED** | Atama & Terfi: `talep` → `ik_incelemede` → `kurulda` → `onayli` → `uygulandi` (also `reddedildi`, `iptal`). Kurul votes. Kart-origin changes tagged `kaynak === "kart"` skip that flow. |
| L | Bulk movement? | **CONFIRMED (şirket)** | Şirket Transferi selects **multiple** personel from source firma. Department/görev card edits are **one personel**. Atama form is one personel. |
| M | Future-dated movement? | **CONFIRMED** | Atama `yururluk` in the future shows “yürürlüğe girecek”; `simdiUygula` applies due approved rows. |

### Tracked card fields (CONFIRMED)

Kart change modal categories: `gorev`, `departman`, `ucret`, `duzelt`.  
Tracked values: `maas`, `maasTipi`, `ucretTipi`, `departman`, `gorev`, `kademe`, `durum`, `isGiris`, `istenCikis`.

### Atama & Terfi types (CONFIRMED UI labels)

| Code | Label | Copy |
|------|-------|------|
| `terfi` | Terfi | Görev yükselmesi (genelde ücretle birlikte) |
| `nakil` | Nakil / Atama | Departman veya firma/şube değişikliği |
| `gorev` | Görev Değişikliği | Yatay görev değişimi (terfi değil) |
| `ucret` | Ücret Değişikliği | Sadece ücret |
| `kademe` | Kademe İlerlemesi | Kademe/derece |
| `gecici` | Geçici Görevlendirme | Bitiş tarihli |

Watched payload fields: `departman`, `gorev`, `firma`, `kademe`, `maas`.

---

## 6. WebİK inferred findings

| Topic | Classification | Note |
|-------|----------------|------|
| Apply step overwrites current personel fields | **INFERRED** | `hareket-uygula` is a server call; FE then expects card fields (`departman`/`gorev`/`firma`/…) to match `yeni`. Snapshot does **not** include portal.js; do **not** treat that as a proven DB temporal model. |
| `gecmis` is an event log on the personel document, not closed Assignment rows | **INFERRED** from FE shape | Reconstruction of department-at-date walks events, then falls back to **current** `p.departman`. |
| Firma ≈ HuGu Property (şube), not always legal employer | **INFERRED** | Multi-firma tenant; şirket transfer creates a **new personel id**. HuGu Employee is Organization-scoped. |
| Görev ≈ HuGu Position (job title), not DutyCode | **INFERRED** | WebİK `gorev` is the card job title; HuGu `EmploymentDutyCode` is official SGK-ish lookup — different concept. |
| Manager is inferred from kademe/title tree | **INFERRED** | Matches “do not derive manager from Position title” as a HuGu **rejection**. |

---

## 7. WebİK not-confirmed / limitations (what HuGu should do better)

| Topic | Classification |
|-------|----------------|
| Server schema / temporal Assignment equivalent | **NOT CONFIRMED** |
| Person-to-person reporting line table | **NOT CONFIRMED** (none found in FE) |
| Legal document generation for nakil/terfi | **NOT CONFIRMED** |
| Whether `hareket` apply is truly atomic with card overwrite | **NOT CONFIRMED** |

**Do not copy:**

1. **Current-state overwrite** of `departman` / `gorev` / `firma` on the personel record as source of truth.
2. **History as a mutable JSON array** on the person, with **Geri Al** that deletes the event and writes old strings back.
3. **String/name-based organization** (departman/görev matched by display name; org chart via `bagliGorev` title).
4. **Kademe-as-manager** (`YONETICI_KADEMELERI`). HuGu: Position/Department never grant permissions; manager is not a title.
5. **Düzeltme that skips history** while still changing current fields — silent loss of reconstructability.
6. **Şirket transfer = new personel identity** inside the same employer when HuGu already has Employee + Employment + Assignment across Properties.
7. **TimeCore “Personel Hareketleri” naming** for organizational moves.
8. **Kurul / BPM in MVP** — useful later; not required to freeze the domain.
9. **Ücret inside organizational movement MVP** — compensation is a different aggregate (HR-04 deferred pay).

**HuGu should do better:**

- Assignment rows as **structural** SoT (already started).
- Semantic movement type/reason/actor as a **first-class event**, not a card-save side effect.
- Effective-dated **person-to-person** reporting line (not title/kademe).
- Same-Organization Property transfer **without** cloning Employee.
- Corrections that **do not destroy** history.
- Dated resolution already used by Puantaj / schedule / leave create — keep it.

---

## 8. Requested movement types — structural vs semantic

Do **not** model all six identically because they share one UI.

| # | Business movement | Class | Notes |
|---|-------------------|-------|-------|
| 1 | Department Change | **A. Structural Assignment** | New Primary Assignment (new DepartmentId; Position only if still applicable). |
| 2 | Position Change | **A. Structural Assignment** | New Primary Assignment (new PositionId; usually same Department). |
| 3 | Promotion | **B. Semantic event** over a Position (often Assignment) transition | Not automatically every Position change. Receptionist → Senior Receptionist **may** be promotion; Receptionist → Night Auditor is usually lateral Position Change. |
| 4 | Duty / Role Change (“Görev Değişikliği”) | **NEEDS VALIDATION** | Collides with Sprint 0.8 Transfer label, WebİK yatay `gorev`, and `EmploymentDutyCode`. See §11. |
| 5 | Property Transfer | **A. Structural Assignment** (same Organization) | New Primary whose Department.PropertyId differs. Cross-Organization is **not** this (see §13). |
| 6 | Manager Change | **C. Reporting-line change** | Independent of Assignment unless the guided flow records both. |

Document-only (print/PDF) is **out of MVP**. Zimmet / demirbaş is **not** Property (tesis) transfer.

---

## 9. Department Change

**Department A → Department B.**

| Question | Recommendation |
|----------|----------------|
| New Assignment? | **Yes.** Close previous Primary D−1; open new Primary on D. Do not mutate old DepartmentId. |
| Effective date? | Property-local `DateOnly`. |
| Position remains? | **Only if** `DepartmentPositionApplicability` holds for (B, current Position). |
| If Position not applicable? | **Reject** unless the user also selects a valid Position in B (atomic Department+Position). Do not silently clear Position server-side. |
| Atomic Department+Position? | **Yes.** Same as today’s Transfer command shape. UI: changing Department clears Position unless still applicable (Personnel Card already does `retainedPositionId`). |

**Invariant:** destination (Department, Position) must be applicable, active, and Property-consistent with the movement’s target Property.

---

## 10. Position Change

**Position A → Position B**, typically same Department.

| Question | Recommendation |
|----------|----------------|
| Same Department? | Default yes; user may also change Department in the same movement (then it is a combined structural change — one new Assignment, semantic type = the user-selected movement type). |
| Applicability | Required. |
| New Assignment vs mutation | **New Assignment.** Never overwrite PositionId on the old row. |
| Effective date | D−1 / D. |
| History | Old Assignment keeps Position A. |
| Future Puantaj | Dated Covering — already correct if Assignment history is used. |

---

## 11. Promotion

**Promotion ≠ automatically Position Change.**

| Option | Meaning | Outcome |
|--------|---------|---------|
| A | `MovementType` stored only as a flag on Assignment | **Rejected.** Assignment is structural; it should not carry HR semantics that two different business events share. |
| B | Semantic `PersonnelMovement` (type `Promotion`) **referencing** the Assignment transition | **Recommended for MVP.** |
| C | Grade / Kademe domain | **Rejected for MVP.** Grade is deferred (HR-01 / HR-04). Do not invent a ladder to justify Promotion. |

MVP rule (proposed, **Q3 for PO**):

- Choosing **Terfi** requires a **different Position** (applicability validated).
- The structural write is the same as Position Change (close + new Assignment).
- The **semantic type** persisted on the movement event is `Promotion`, so the timeline says Terfi, not Pozisyon Değişikliği.
- Promotion without Position change (title-only / grade-only) is **out of MVP**.

Do **not** auto-change `OfficialEmploymentProfile.OccupationCode` (already HR-03: HR may edit afterwards).

---

## 12. Duty / Role Change — Görev Değişikliği

**NEEDS VALIDATION. Do not invent a string field.**

Possible interpretations in this repository:

| Interpretation | Status |
|----------------|--------|
| Job title / Position change | Already **Position Change** |
| Sprint 0.8 UX label for Transfer | Product language debt, not a type |
| WebİK yatay `gorev` (not terfi) | Capability reference; HuGu Position Change covers this |
| `EmploymentDutyCode` (SGK-ish görev kodu) | Official profile lookup — **not** operational movement |
| Authorization / ApplicationUser Role | **Forbidden** to mix (Employee ≠ User ≠ Role ≠ Position) |
| Temporary / joker Assignment | `AssignmentKind.Temporary` conceptual only |
| Secondary duty / matrix | Not modeled |

**MVP recommendation:** **defer** Görev Değişikliği as a distinct movement type until PO freezes meaning (Q4). Use **Pozisyon Değişikliği** and **Terfi** in the new module. Retire or relabel Personnel Card “Görev değişikliği” when HR-08 UI is authorized so the term is not used for two things.

---

## 13. Property / tesis transfer

### Same Organization, different Property (Ankara Hotel → İstanbul Hotel)

**CTO recommendation: Assignment transfer. Employment stays.**

This is already how the write model works when workplace = destination Property:

- Same `Employee` (Organization-scoped `PersonnelNumber` stays unique and stable).
- Same `Employment` (one work relationship with the employer).
- New Primary Assignment whose Department belongs to İstanbul.
- Historical Ankara Assignment retained; Puantaj/schedule dated resolution already splits the month.

Department and Position **must be reselected** (Property-scoped master data; applicability is per Property). Do not copy Ankara Position id.

Active Property context: the operational write should authorize **both** source and destination (see §18). Do not rely on “switch cookie to İstanbul and close Ankara Assignment” as the product rule even though the current Transfer use case behaves that way.

### Cross-Organization

**Do not silently reuse Employment.**

| Boundary | Rule |
|----------|------|
| Same Organization, other Property | Movement (Assignment). |
| Employment ended, later hired again | **Rehire** — new Employment (UI still deferred, invariant already exists). |
| Other Organization | New Employee membership in that Organization (new `PersonnelNumber` uniqueness scope) + new Employment. Not HR-08 Property Transfer. Hotel Group / cross-employer is **not** implemented. |

SGK işyeri / legal workplace may change even inside one Organization; that is OfficialEmploymentProfile / SgkWorkplaceRegistration, **not** automatically a Property Transfer, and **not** this MVP.

---

## 14. Manager / reporting line

**Required discovery decision.** No reporting-line entity exists today (HR-DOMAIN-001 deferred `ReportsToEmployeeId`; HR-04 sketched future `EmployeeReportingLine`).

### First principles

- “Who is the manager **on a given date**?” must be reconstructable.
- Manager is a **workforce** relationship, not an authorization role.
- Do **not** attach manager to `ApplicationUser`.
- Employee ≠ ApplicationUser.
- Do **not** derive manager from Position title, kademe, or names (“Müdür”, “Manager”).
- Do **not** hardcode title lists.

### Options

| Option | Historical | Property/dept transfer | Future-dated | Multiple employments | Temporary manager | Matrix | Leave / performance | Cycles |
|--------|------------|------------------------|--------------|----------------------|-------------------|--------|---------------------|--------|
| **A** `Employment.ManagerEmploymentId` | Overwrites | Survives Assignment change; no history of *who was* manager | No | Scoped to employment | Mutates | No | Current only | Easy to check now, not as-of |
| **B** `Assignment.ManagerAssignmentId` | Tied to posting | Manager’s own transfer **breaks** all subordinates | Awkward | Coupled to Assignment lifetime | Forces Assignment churn | Over-fits | Fragile | Assignment graph cycles |
| **C** `Employee.ManagerEmployeeId` | Overwrites | Ignores employment periods / rehire | No | Wrong grain | Mutates | No | Current only | Easy now |
| **D** Separate effective-dated `ReportingLine` | **Yes** | Independent of Assignment; optional combined movement | **Yes** | Per Employment | Close + open | Defer extra rows | Resolve covering date | Check as-of graph |

### Final CTO recommendation: Option D, Employment-to-Employment, Primary only

Proposed conceptual entity (do **not** implement now):

```text
WorkforceReportingLine
  OrganizationId
  SubordinateEmploymentId
  ManagerEmploymentId
  EffectiveFrom   (DateOnly, Property-local calendar of the subordinate workplace)
  EffectiveTo     (DateOnly?, inclusive)
  Kind = Primary  (MVP)
```

**Why reject A:** current-state FK cannot answer “who was the manager in March.” Same class of bug Assignment was invented to avoid.

**Why reject B:** the manager is a **person in an Employment**, not a posting. If the manager transfers department, subordinates should still report to that person unless a Manager Change says otherwise. Assignment ids would require rewriting every subordinate line on every manager transfer.

**Why reject C:** Employee outlives sequential Employments; a rehire must not silently inherit a manager; current-state cannot support dated Puantaj/leave/performance.

**MVP constraints:**

- One **primary direct manager** at a time per subordinate Employment (non-overlap, D−1 / D).
- Manager Employment must be non-ended on D (or covering D) in the **same Organization**.
- Self-manager forbidden (`SubordinateEmployment.EmployeeId != ManagerEmployment.EmployeeId`).
- Cycle forbidden on the as-of graph of Employments.
- Manager **may** work at another Property of the same Organization (hotel reality: cluster GM). **NEEDS VALIDATION** whether MVP should restrict same-Property only.
- Department Change / Property Transfer **does not** auto-rewrite reporting line. The guided flow should **ask** if manager changes; if yes, write both events in one transaction.

**Deferred:** matrix / dotted line, org-chart engine, department-head inference, “manager of department X” as a Position flag, ApplicationUser linking, leave-approval routing by manager (HR-05B still department-scope + HR).

---

## 15. Historical timeline / movement-history model

### Options

| Model | What | Complexity | Semantic Promotion vs Position | User timeline | Documents / audit | Duplication |
|-------|------|------------|--------------------------------|---------------|-------------------|-------------|
| **1** Assignment history only | Reconstruct from Assignment (+ later ReportingLine) | Lowest | **Cannot** | Weak (no reason/actor/type) | Weak | None |
| **2** Assignment + lightweight `PersonnelMovement` event | Assignment remains structural SoT; event stores type, reason, note, actor, previous/new Assignment ids, effective date | Medium | **Yes** | Strong | Strong | Small (ids + labels) |
| **3** Generic movement aggregate **owns** structural changes | Assignment becomes a projection of Movement | High | Yes | Strong | Strong | Risk of two SoTs |

### Final recommendation: Model 2

Assignment (and ReportingLine) remain the **reconstructable organizational facts** Puantaj already depends on.

`PersonnelMovement` (English type name; UI: Personel Hareketi) is an **immutable business event**:

```text
PersonnelMovement
  OrganizationId
  EmploymentId
  MovementType          (DepartmentChange | PositionChange | Promotion | PropertyTransfer | ManagerChange)
  EffectiveDate         (DateOnly)
  PreviousAssignmentId?
  NewAssignmentId?
  PreviousReportingLineId?
  NewReportingLineId?
  Reason? / Note?
  ActorUserId
  CreatedAtUtc
```

**Do not** make this a generic event store. **Do not** add it automatically for Hire/End Employment in this slice (those remain Hire / End Employment commands; they may appear on a later unified timeline).

Hire and End Employment already create/close Assignments without this event. HR-08 MVP can still **list** Assignment-only history as “structural periods” plus movement events where they exist — or backfill is **out of scope**.

---

## 16. Effective dates

- Business date = Property-local `DateOnly` (destination Property timezone for the movement).
- Audit timestamps = UTC `DateTimeOffset`.
- Rule: effective **D** → old Primary covers through **D−1**; new Primary from **D**. No timestamp split.

### Future-dated (proposed MVP — **Q2**)

**Include** future-dated movements in MVP: the Assignment model already stores a future close + successor without overlap.

| Topic | MVP |
|-------|-----|
| Schedule next month’s transfer? | **Yes.** |
| When does “current Assignment” switch? | `Covering(today)` — old row until D−1 inclusive. |
| Future Assignment coexist? | **Yes** (closed current with future EndDate + successor StartDate = D). |
| Overlap | Still forbidden. |
| Cancel before effective date? | **Yes** — cancel event; restore previous `EndDate = null`; do not leave an orphan future Primary. Only if the successor has **no** dependent schedule/leave/attendance writes. |
| Apply job? | Not required if rows are written at save time (current Transfer style). Optional later “pending” status is WebİK-like BPM — **defer**. |

Past-dated movements (corrections of last week) are **allowed** with the same D−1/D rule, subject to overlap and employment bounds. Explain Puantaj/schedule consequences in UX (open months still derive from Assignment).

---

## 17. Correction / reversal / deletion

Avoid destructive edit of old history (do not copy WebİK Geri Al).

| State | Allowed |
|-------|---------|
| Future movement, not yet effective, no dependents | **Cancel** (structured); Assignment pair rolled back as above |
| Effective movement | **No hard delete.** **No** mutate DepartmentId/PositionId on old rows |
| Mistake after effective date | **Reversal / correction movement**: close current Primary on D−1 of the correction, open corrected successor; movement type `Correction` or a reversal referencing the original movement id (**Q8**) |
| Reason/note typo | Limited metadata edit **NEEDS VALIDATION**; default **no** |
| Hire-date same-day wrong department | Still blocked by D−1; remains a known gap (Sprint 0.8). Do not silently break the invariant in HR-08. |

Lifecycle: `Scheduled` (future) → `Effective` → (`Cancelled` only from Scheduled) | (`Superseded` via new movement).

---

## 18. Authorization

Proposed permission codes (do **not** implement):

| Code | Intent |
|------|--------|
| `hr.movements.read` | See movement list / timeline |
| `hr.movements.manage` | Create / cancel-before-effective |
| `hr.movements.approve` | **Catalogued for later**; not granted in MVP |

No role-name checks. No Position-title checks.

### Scope

Movements are **highly sensitive**. They are **not** department-scheduler work in MVP.

| Operation | Rule |
|-----------|------|
| Same-Property Department/Position/Promotion/Manager | Active Property required. `hr.movements.manage`. **Property-wide HR** (AUTH-02 department scopes **do not** apply to manage). Read may later be department-narrowed; **not** required to freeze manage. |
| Property Transfer | Actor must have authorized **management scope for both source and destination Properties**, **or** an Organization-wide membership with `hr.movements.manage`. |
| Cross-Organization | Out of this module. |

**Q7 freeze recommendation:** source **and** destination, or org-wide HR. A user who can manage Ankara but not İstanbul **cannot** transfer Ankara → İstanbul.

Current Transfer **fails this recommendation** (destination-only). Discovery must not change that behavior; implementation later must close the hole.

---

## 19. Puantaj impact

HR-07 already resolves **Assignment applicable on target LocalDate**. Proposed movements that only add Assignment rows with D−1/D **preserve** this.

Example: transferred 15 Sep → 1–14 old Department/Property/Position; 15–30 new.

**No current Puantaj code identified that would break** if Transfer semantics stay. `GetAttendanceMonthQuery` uses `EffectiveAssignmentResolver`; other-Property days are `OutOfScope`.

Risks (document, do not implement):

- Future-dated close of the current Primary is already how Transfer works; Puantaj `Covering` remains correct.
- Open-month roster still derives until period lock (ADR-011) — transferring yesterday changes historical context for derived days. Same as today’s Transfer.
- Do **not** copy DepartmentId onto AttendanceCorrection as SoT; keep AssignmentId (already).

---

## 20. Leave impact

| Fact | Behavior |
|------|----------|
| `LeaveRequest.AssignmentId` | Snapshotted at **submit** from StartDate; client cannot send it. Range must lie in **one** Assignment interval. Cross-Assignment create is rejected. |
| `LeaveRecord` | **No** AssignmentId. Employment + date range. Puantaj paints leave by calendar coverage, then workplace comes from **dated** Assignment. |
| AUTH after transfer | Leave request workplace uses **persisted** request Assignment, not current posting (`LeaveRequestWorkplaceAccess`). |

**Risk (NEEDS VALIDATION):** create a pending LeaveRequest, **then** Transfer with D inside that range. Create-time invariant is not re-checked. Request.AssignmentId can disagree with Covering(date) after D. Approval still authorizes the **old** department.

**Do not redesign Leave in HR-08.** Proposed guard when movements are implemented: reject (or require withdraw) a movement that splits a **pending** LeaveRequest range. Recorded `LeaveRecord` stays; dated Puantaj context follows Assignment (usually desired).

Leave requested before transfer for dates **entirely after** D: submit-time Assignment is the **then-current** Primary; after transfer those dates belong to the new Assignment. Same class of stale snapshot. Flag for PO: should HR-08 block such transfers until leave is resubmitted? Default recommendation: **block pending requests that would cross the new boundary**; do not auto-split.

---

## 21. Performance impact (future)

Do **not** implement Performance.

Manager evaluation must resolve manager from **ReportingLine covering a chosen date**, not “current manager,” not ApplicationUser, not Position title.

**Recommended default (not frozen):** manager as of **evaluation period end** (Property-local). Document alternatives (period start, evaluation date) when that module is discovered.

If only `Employment.ManagerEmploymentId` existed, historical reviews would be impossible after a Manager Change.

---

## 22. Proposed top-level UX

Do **not** implement UI.

**Personel Hareketleri** main page:

**Header filters:** Property (active context + optional source/destination for transfers), date range (effective date), movement type, department, employee search.

**Primary action:** Yeni Hareket.

**Main list:** Employee, Movement Type, Previous, New, Effective Date, Reason, Actor.

**Tabs:** **not** required if filters include type. Avoid Tüm / Terfiler / Transferler tabs unless the list is proven noisy.

Empty state: no movements in range. Future-dated rows visually distinct.

---

## 23. Proposed New Movement flow

One guided workflow with **conditional fields**:

1. Personel (active Employment)
2. Hareket Türü
3. Geçerlilik tarihi
4. Destination fields by type:
   - Department Change → new Department; Position kept or reselected if inapplicable
   - Position Change / Promotion → new Position (Promotion copy explains semantic difference)
   - Property Transfer → destination Property + Department + Position; optional Manager
   - Manager Change → new manager Employment
5. Reason (required)
6. Note (optional)
7. Review (previous → new, D−1 / D in product language, not jargon)
8. Save (one transaction)

Do **not** ship six separate forms. Do **not** put this on Personnel Card as the create surface.

---

## 24. Personnel Card relationship

- Top-level module owns **create/manage**.
- Card **Organizasyon** keeps a **read-only** current Property/Department/Position.
- Existing card Transfer action should later **deep-link** to Yeni Hareket with personel preselected, or be removed so there is one write path.
- Card **Geçmiş** may show read-only movement history in **HR-08B** (**Q9**).
- Do not add a competing card tab “Hareketler” in MVP if the top-level list exists; a compact read-only block is enough later.

---

## 25. MVP scope

**In HR-08 MVP (proposed):**

- Top-level module identity and IA (when UI is authorized)
- Movement types: **Department Change**, **Position Change**, **Promotion** (semantic over Position transition), **Property Transfer** (same Organization), **Manager Change** (effective-dated ReportingLine)
- Assignment close+create as today (D−1 / D)
- Lightweight `PersonnelMovement` event
- Effective-dated Primary ReportingLine
- Reason + actor + UTC audit
- Future-dated + cancel-before-effective
- Source+destination authorization for Property Transfer
- Permissions `hr.movements.read` / `hr.movements.manage`

**Out / deferred:**

- Görev Değişikliği as its own type until Q4
- Grade / Kademe
- Temporary / joker Assignment UI
- Matrix reporting / org chart
- Approval workflow (`hr.movements.approve`)
- Bulk department reorganization
- Movement documents / PDFs
- Ücret inside this module
- Zimmet
- Cross-Organization transfer
- Rehire UI
- Same-day hire-date correction (invariant stays)
- Physical delete of effective history
- ApplicationUser manager
- Title-based manager inference
- Menu/sidebar until a UI slice is authorized

---

## 26. Non-goals

- No production code, entity, DbSet, migration, API, frontend route, sidebar, or permission seed in **this** discovery.
- Do not change Transfer / Assignment behavior in this slice.
- Do not implement ReportingLine or PersonnelMovement now.
- Do not copy WebİK Atama & Terfi BPM or kart Geri Al.
- Do not treat TimeCore Personel Hareketleri as this module.
- Do not build overtime/payroll (ADR-011’s older “HR-08” leftover).

---

## 27. Transaction boundary

Any movement that changes Assignment:

- Close old Primary (D−1) + insert new Primary (D) + insert `PersonnelMovement` (+ ReportingLine close/open if manager changes) in **one** `SaveChanges`.
- Reject partial save (gap or overlapping Primaries).
- Validate applicability **before** close.
- Property Transfer: validate both Properties and destination applicability in the same transaction.

---

## 28. Rehire vs movement boundary

| Case | Mechanism |
|------|-----------|
| Same Organization, active Employment, other Department/Position/Property/Manager | **Movement** |
| Employment ended, later returns | **New Employment** (rehire) — not HR-08 |
| Other Organization | New Employee + Employment in that org — not HR-08 |

---

## 29. Bulk movements

Hotels reorganize departments in bulk. **Not MVP.** Architecture (Assignment + event) does not make bulk trivial (applicability, dual-Property auth, leave guards). Future consideration only.

---

## 30. Open questions / NEEDS VALIDATION

See also §31 PO list.

- Exact product meaning of **Görev Değişikliği** (Q4).
- Pending LeaveRequest vs later Transfer (stale AssignmentId).
- Manager same-Property-only vs cross-Property manager in MVP.
- Whether Reason is a closed code list vs free text.
- Limited metadata edit after save.
- Whether Hire/End Employment should emit the same event type later.
- Seniority / SGK işyeri side effects of Property Transfer (HR-04 already flagged seniority).
- Same-day correction on assignment start date (Sprint 0.8).

---

## 31. PO decisions required — **Accepted 2026-09-04**

| Id | Question | Decision |
|----|----------|----------|
| **Q1** | Can Property Transfer happen only inside the same Organization? | **Accepted: Yes.** Cross-Organization is End+new Employment / new org membership, not this movement. |
| **Q2** | Future-dated personnel movements in MVP? | **Accepted: Yes**, with cancel-before-effective. |
| **Q3** | Should Promotion require a Position change? | **Accepted: Yes** for MVP. No Grade. Semantic type separate from Assignment. |
| **Q4** | What does “Görev Değişikliği” mean in HuGu? | **Accepted: Defer as a type.** Use Position Change / Promotion. Relabel card Transfer in HR-08B. |
| **Q5** | Only one direct manager at a time in MVP? | **Accepted: Yes.** Matrix deferred. |
| **Q6** | Should Manager Change be effective-dated and historical? | **Accepted: Yes.** Separate ReportingLine. |
| **Q7** | Who can perform Property Transfer? | **Accepted: Source and destination** authorized manage, **or** org-wide HR. |
| **Q8** | Can effective movements be corrected, or only reversed with a new movement? | **Accepted: Reverse/correct with a new movement.** No destructive edit. Cancel only if not yet effective. |
| **Q9** | Personnel Card read-only movement history in HR-08B? | **Accepted: Yes**, later. Manage stays top-level. Deep link from card. |
| **Q10** | Movement approval workflow now or later? | **Accepted: Later.** Catalog `hr.movements.approve`. MVP is HR-managed save. |

---

## HR-08A implementation notes

**HR-08A Accepted / Completed** (2026-09-04) after live PO/CTO acceptance: DepartmentChange, PositionChange, Promotion, ManagerChange, future movement + cancel, same-Organization PropertyTransfer, dual-Property authorization, legacy Personnel Card Transfer API, list/detail DTOs, Problem Details, dated Puantaj, and shared active-Property request context.

**HR-08B Accepted / Completed** (2026-09-05) after live PO/CTO acceptance: top-level Personel Hareketleri UI, wizard, detail drawer, card history, Finding 05 hierarchy, and brand integration.

**HR-08 overall:** **Completed**.

See [ADR-012 HR-08A implementation freeze](../../architecture/adr/ADR-012-Workforce-Movements-And-Reporting-Line.md) for lifecycle derivation, cancel/delete of never-effective Assignment rows, legacy Transfer type mapping (`AssignmentChange`), leave/schedule block policies, dual-Property authorization, and permission grants.

---

## Related documents

- [ADR-012](../../architecture/adr/ADR-012-Workforce-Movements-And-Reporting-Line.md)
- [WORKFORCE_MODEL.md](../../domain/hr/WORKFORCE_MODEL.md)
- [INVARIANTS.md](../../domain/hr/INVARIANTS.md)
- [HR-04](HR-04-Employment-Working-Conditions.md)
- [HR-05B](HR-05B-Leave-Request-Approval.md)
- [HR-06](HR-06-Shift-Work-Schedule.md)
- [HR-07](HR-07-PUANTAJ-DISCOVERY.md)
- [ADR-011](../../architecture/adr/ADR-011-Puantaj-Domain-Model.md)
- [AUTH-02](../../security/authorization/DEPARTMENT_MEMBERSHIP_SCOPE.md)
- [AUDIT.md](../../architecture/foundation/AUDIT.md)
- [TIME_AND_TIMEZONE.md](../../architecture/foundation/TIME_AND_TIMEZONE.md)
