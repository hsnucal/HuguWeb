# HR-04 — Çalışma Bilgileri / Employment & Working Conditions

> **Status:** Accepted — Product Owner accepted implementation (2026-08-29).
>
> Domain freeze (ownership, MVP, termination model, exclusions) remains **Accepted**. Implementation is **Accepted**.
>
> **Does not supersede** HR-DOMAIN-001, HR-DOMAIN-002, or HR-DOMAIN-003. Those remain **Accepted**.
>
> **WebİK** remains a capability reference only.

---

## Slice identity

Keep:

| | |
|--|--|
| Slice id | **HR-04** |
| EN | Employment & Working Conditions |
| TR | Çalışma Bilgileri |

Do **not** rename this feature because older planning documents used the number for Documents (Belgeler / Evraklar).

Older Accepted Personel Master / Official Employment text that says “HR-02 = entry/exit” and “HR-04 = Documents” is **not rewritten**. Planning reconciliation lives in [README.md](README.md). Documents remain a **later, different** slice.

---

## Frozen domain ownership

| Concept | Owns | Must not own |
|---------|------|----------------|
| **Employee** | Person identity (`Id`, `OrganizationId`, `GivenName`, `FamilyName`, `PersonnelNumber`) | Employment dates, status, contract, department, position, property, manager |
| **Employment** | Relationship / lifecycle **and** current contractual snapshot | Attendance, SGK submit state, wage, Assignment rows, PropertyId |
| **Assignment** | Effective-dated organizational / workplace assignment | Permissions, statutory meslek kodu, IBAN, manager |
| **OfficialEmploymentProfile** | Official / SGK-related classification for **this** Employment | Exit workflow, working hours, contract type editing |
| **EmployeePaymentProfile** | Bank / payment destination only | Employment lifecycle |
| **ApplicationUser** | Login | Hire / End Employment |

`Employee ≠ ApplicationUser`. Hiring must not create a login. Ending employment must not delete Identity.

Do **not** duplicate employment fields onto Employee.

```text
Employee                         [identity]
  ├── EmployeeHrProfile          1:0..1   (unchanged)
  ├── EmployeePaymentProfile     1:0..1   (unchanged — out of HR-04)
  └── Employment                 1..*     (at most one non-ended)
        ├── StartDate, EndDate?, Status
        ├── SeniorityStartDate?          ← MVP
        ├── TerminationReason?           ← MVP, set only by End Employment
        ├── ContractType?
        ├── ContractEndDate?
        ├── PartTimeMonthlyHours?
        ├── OfficialEmploymentProfile    1:0..1
        ├── EmploymentBesSettings        1:0..1   (out of HR-04 UI)
        └── Assignment                   1..*
              DepartmentId, PositionId, period, Kind

Workplace (display) = current/last Primary Assignment → Department.PropertyId → Property
```

Do **not** add `Employment.PropertyId`. Do **not** add `Employee.PropertyId`.

---

## Frozen Assignment behavior

Preserve effective-dated Assignment history.

- Department / Position changes must **not** overwrite a historical Assignment row.
- Transfer on date D: previous Primary ends **D−1**, new Primary starts **D**.
- Primary assignments must not overlap.
- Property / workplace remains derived through `Assignment → Department.PropertyId`.
- Property transfer, when later authorized, uses the same Assignment history — not a PropertyId on Employee or Employment.
- `AssignmentKind.Temporary` stays in the model; **no UI in HR-04**.

Existing `TransferEmployee` remains the organizational-change workflow.

---

## HR-04 MVP (frozen)

### Existing (already in domain; compose on Çalışma Bilgileri)

- Employment status (`Scheduled` / `Active` / `Ended`)
- `Employment.StartDate`
- `Employment.EndDate` **display** (not a free edit)
- Property / workplace **display**
- Department (via current Primary Assignment; mutate only via Transfer)
- Position (same)
- `ContractType`
- `ContractEndDate`
- `PartTimeMonthlyHours`

### New

- optional `Employment.SeniorityStartDate` (TR: **Kıdeme Esas Tarih**)
- `TerminationReason` captured **only** through End Employment

### UX

- Rename tab **Çalışma / organizasyon** → **Çalışma Bilgileri**
- Reorganize into the sections in [Personnel Card](#personnel-card-ux-frozen)
- Contract editing **moves here** (single editable owner)
- Existing Transfer action remains
- Existing End Employment action remains and is extended

---

## Contract editing — frozen PO decision

Authoritative **edit** location for:

- `ContractType`
- `ContractEndDate`
- `PartTimeMonthlyHours`

is **Çalışma Bilgileri → Sözleşme**.

Remove duplicate **editing** of these fields from Resmî → İŞKUR.

Resmî / İŞKUR **may** display them **read-only** if that screen genuinely benefits. Do **not** maintain two editable forms for the same Employment fields.

İŞKUR-only fields (`IskurStatus`, `IskurWorkforceStatus`, incentive dates) stay on Resmî. Work-permit dates stay on Resmî → Sosyal. Do not move them to Çalışma Bilgileri.

**MVP contract model:** current Employment snapshot only. Do **not** introduce `EmploymentContract`. History of multiple contractual periods (seasonal renewals, fixed-term → indefinite, repeated seasons) is deferred until product need is proven. Field-change audit is **not** a substitute for that later period model.

Do **not** introduce `WeeklyHours`. Keep `PartTimeMonthlyHours` where it already has meaning (part-time contractual hours). Weekly contractual hours stay deferred.

---

## SeniorityStartDate — frozen

Optional `DateOnly?` on Employment.

TR: **Kıdeme Esas Tarih**.

### Persistence vs UX prefill

Create/hire UI **may suggest** `Employment.StartDate` in the input. Do **not** silently persist a second stored value unless the save payload explicitly includes `SeniorityStartDate`.

If omitted, the column stays **null**. Later consumers (leave / compensation) should treat effective seniority as:

```text
effectiveSeniorityDate = SeniorityStartDate ?? StartDate
```

That fallback is **computed**, not an implicit insert.

### Precise invariant (implementation must enforce)

1. `SeniorityStartDate` is optional. Null is valid for open and ended employments.
2. When present, it is a calendar date (`DateOnly`).
3. When present: **`SeniorityStartDate <= Employment.StartDate`**. Prior service is an **earlier** date, not a later one. Equality with `StartDate` is valid (no extra prior service).
4. When `Employment.EndDate` is set: **`SeniorityStartDate <= EndDate`** (follows from (3) plus existing `EndDate >= StartDate`).
5. The field does **not** replace `StartDate` as the employment-relationship start.
6. Changing it on an open employment is a current-state edit (optional `PersonnelProfileChange` audit). It is **not** a Transfer and **not** a new Employment.

**NEEDS HOTEL VALIDATION:** how hotels interpret this date on transfer / rehire. Does **not** block HR-04 MVP.

---

## Termination — frozen

Termination is a **domain action**. Extend existing **End Employment**.

Do **not** expose `Employment.EndDate` as an ordinary editable field.

The action captures at minimum:

| Input | Required |
|-------|----------|
| `EndDate` | Yes (existing: `>= StartDate`; closes Employment to `Ended`; closes applicable Primary Assignments) |
| `TerminationReason` | Yes (HuGu business reason; see below) |
| Optional note | **Defer.** `HrNotes` is person-level; `PersonnelProfileChange` is profile-field audit. Do **not** invent `Employment.TerminationNote` in MVP |

Do **not** add “Undo Termination” to HR-04 MVP. Do **not** make `EndDate` / `Status` manually editable as a workaround.

Mistaken termination correction is a **future explicit administrative workflow**. That prevents bypassing lifecycle rules.

Ending employment must **not** automatically disable or delete `ApplicationUser`. Employee ≠ User. A non-blocking reminder if a linked ERP account exists may be shown later; Identity action stays separate.

SGK / İŞKUR exit codes are **not** part of this action.

---

## Termination reason — frozen PO decision

HuGu termination reason is an **HR / business** concept. It is **not** SGK EK-2 and **not** İŞKUR ayrılış kodu.

Do **not** reuse official exit-code catalogues. Do **not** make application behavior depend on localized display text.

### Reference-data approach (simplest, consistent with ARCH-01)

[REFERENCE_DATA.md](../../architecture/foundation/REFERENCE_DATA.md) Category **A** — domain enum / closed state — same family as `EmploymentStatus` and `EmploymentContractType`.

Do **not** build a generic universal lookup framework. Do **not** add these codes to `OfficialLookupCatalog` (that catalogue is official/SGK).

Conceptual type: `EmploymentTerminationReason`. Persist the **stable English identifier**. Labels live in `tr` / `en` / `ru` i18n.

| Code (stored) | TR | EN | RU direction |
|---------------|----|----|----------------|
| `Resignation` | İstifa | Resignation | Отставка / увольнение по собственному желанию |
| `EmployerTermination` | İşveren feshi | Employer termination | Расторжение по инициативе работодателя |
| `ContractEnded` | Sözleşme sonu | Contract ended | Окончание договора |
| `Retirement` | Emeklilik | Retirement | Выход на пенсию |
| `Other` | Diğer | Other | Другое |

Required on End Employment. Stored on that Employment. Unset on a non-ended Employment.

If hotels later need **custom** extra reasons, promote this explicit family the same way `EmploymentDutyCode` was promoted (codes + localized labels, `IsActive`) — still not a generic lookup platform, and still not SGK EK-2.

---

## Status — frozen

Keep:

| State | Meaning |
|-------|---------|
| `Scheduled` | StartDate in the future; not ended |
| `Active` | Relationship in force |
| `Ended` | Closed by End Employment; has EndDate |

Hybrid already implemented: `Ended` is **stored** by command; `Scheduled` vs `Active` is **derived** (`EffectiveStatus` / `RefreshLifecycle`).

Do **not** introduce `Suspended`, `Askıda`, or `OnLeave` as `EmploymentStatus`. Leave and temporary workforce state belong later.

---

## Manager / reporting line — deferred

Do **not** add manager to Employment or Assignment in HR-04.

Do **not** derive manager from Position, Department, Role, Permission, or Grade.

Future direction (not MVP): effective-dated `EmployeeReportingLine`.

---

## Personnel Card UX (frozen)

Rename the existing tab. Do not add a competing tab. Do not move these fields onto Genel bilgiler.

| TR | EN | RU |
|----|----|-----|
| Çalışma Bilgileri | Employment | Трудовая информация |

### Sections

**A. İstihdam**

- Status (read-only; derived/stored as today)
- Start date (read-only after hire)
- Seniority date (optional edit)
- End date — **display only when ended**

**B. Sözleşme**

- Contract type (authoritative edit)
- Contract end (when FixedTerm)
- Part-time monthly hours (when PartTime)

**C. Organizasyon**

- Property (read-only display)
- Department (read-only after hire)
- Position (read-only after hire)
- **Transfer** action (existing command)

**D. İşten Ayrılma**

- Only when lifecycle makes it relevant
- If open and permitted: **End Employment** action
- If already ended: reason + end date display (read-only)

Do not make this a giant flat form. Create-mode hire inputs (start date, department, position) remain the existing Hire composition; do not introduce a second write path.

---

## Explicitly deferred / out of HR-04

- Manager / reporting line (`EmployeeReportingLine`)
- `EmploymentContract` history
- Probation date
- Rehire eligibility / rehire UI
- Temporary / joker assignments
- `OriginalCompanyStartDate`
- Weekly hours
- Workforce / personnel groups / Çalışma Grubu / `EmploymentClassification`
- Grade / Kademe
- Bölüm / department hierarchy
- SGK / İŞKUR exit codes and submissions
- Suspended / Askıda as employment status
- Employment documents (Belgeler / Evraklar)
- Salary / wage / payroll
- Payment-profile changes
- Leave, shifts, attendance, puantaj, overtime
- Training, performance, discipline, assets

OfficialEmploymentProfile remains separate. WebİK flat Personel Kartı ownership, in-place Department/Position overwrites, ordinary editable EndDate, and WebİK status/UI design are **not** copied.

---

## Permissions

Ordinary employment facts are **NORMAL HR DATA**. Existing `hr.employee.read` / `hr.employee.manage` for card composition; existing `workforce.manage` for Hire / Transfer / End.

Do **not** require `hr.employee.sensitive.read` for these fields. Do **not** add `hr.official.*` or `hr.employment.*`.

---

## Validation (frozen for implementation; not implemented here)

| Rule | Status |
|------|--------|
| `EndDate >= StartDate` | Existing `TryEnd` |
| `ContractEndDate` required iff `ContractType == FixedTerm` | Existing |
| `PartTimeMonthlyHours` required and `> 0` iff `PartTime` | Existing |
| `ContractEndDate >= Employment.StartDate` when both present | Add in HR-04 implementation |
| `SeniorityStartDate <= StartDate` when seniority present | Add |
| Termination reason required on End Employment; must be a known `EmploymentTerminationReason` code | Add |
| EndDate / Status not writable except via End Employment | UX + API invariant |
| Transfer overlap / D−1 / applicability | Existing |

---

## NEEDS HOTEL VALIDATION (does not block MVP)

1. **SeniorityStartDate semantics** — especially transfer and rehire (new Employment vs credited prior service).
2. Named **supervisor** vs department head (feeds later `EmployeeReportingLine`, not this slice).
3. Whether seasonal hotels will need **retained contract-period history** (`EmploymentContract`) in year one, vs overwriting the snapshot.
4. Operational importance of a **probation end date** on the personnel file.
5. Whether **Askıda** is a real employment lifecycle state for target hotels, or only unpaid/long leave (must stay out of `EmploymentStatus` until that evidence exists).

---

## Closed PO / CTO decisions (formerly open)

| Question | Decision |
|----------|----------|
| Slice id | **HR-04** Employment & Working Conditions. Documents keep the older planning alias; Accepted Personel Master text is not rewritten |
| Contract field UI | **One editable owner:** Çalışma Bilgileri. Resmî may be read-only |
| Termination reason list | Closed HuGu codes above; **not** SGK EK-2; not hotel-maintained in MVP |
| Undo / correct termination | **Out of MVP.** Future administrative workflow |
| `EmploymentContract` child | **Out of MVP** |
| Manager | **Out of MVP** |
| Weekly hours | **Out of MVP** |
| Askıda / Suspended | **Out of EmploymentStatus** |

---

## Implementation notes (Accepted)

Implemented against the frozen decisions below. No frozen rule was reinterpreted. Product Owner accepted this slice on 2026-08-29, including the Personnel Card UX/Data Quality repair.

| Item | Actual |
|------|--------|
| Migration | `AddEmploymentWorkingConditionsHr04` (`20260828110314`) |
| Schema | `Employments.SeniorityStartDate` (`date`, nullable); `Employments.TerminationReason` (`varchar(32)`, nullable, English code); check `CK_Employments_SeniorityStartDate` (`SeniorityStartDate IS NULL OR SeniorityStartDate <= StartDate`) |
| Unrelated schema | None |
| Termination reason | Domain enum `EmploymentTerminationReason` (`Resignation`, `EmployerTermination`, `ContractEnded`, `Retirement`, `Other`). Not SGK EK-2. Not `OfficialLookupCatalog`. |
| End Employment | `POST /api/workforce/employees/{id}/end-employment` now requires `endDate` + `terminationReason`. Ordinary PUT cannot set `Status`, `EndDate`, or `TerminationReason`. |
| Seniority | Optional. Null is stored when omitted. Consumers: `SeniorityStartDate ?? StartDate`. Hire does not copy `StartDate` into the column. |
| Contract edit owner | Personnel Card **Çalışma Bilgileri → Sözleşme**. Resmî → İŞKUR **no longer shows** these three fields (removed rather than disabled duplicates; İŞKUR still has `IskurWorkforceStatus` / incentive fields). |
| Permissions | Existing `hr.employee.read` / `hr.employee.manage` / `hr.employee.sensitive.read` / `workforce.manage`. No new permission. |
| List columns | No new default Personnel List columns. |
| Deviations | None vs freeze, other than İŞKUR display choice B (omit redundant contract fields instead of read-only copies). |

Personnel Card UX/Data Quality (accepted with this slice): Payment Information lives only on **Ödeme Bilgileri** (create and edit); İl/İlçe are dependent dropdowns from the canonical Turkish catalogue; emergency contact phone uses the shared personnel phone format; existing-employee Organizasyon is a read-only label/value layout; date inputs require a 4-digit year.

---

## Appendix A — Current baseline (discovery evidence)

Retained so implementation does not re-audit from scratch. Not a second decision record.

### A.1 Commands and APIs already implemented

| Command | Use case | Writes |
|---------|----------|--------|
| Hire | `HireEmployeeUseCase` / `HireEmployeeWithProfileUseCase` | Employee + Employment + Primary Assignment |
| Update profile | `UpdateEmployeeHrProfileUseCase` | Employee rename + HR profile + official profile + workforce-terms overwrite + BES |
| Transfer | `TransferEmployeeUseCase` | Close previous Primary D−1, open new Primary D |
| End Employment | `EndEmploymentUseCase` | `TryEnd(endDate)` → `Ended`; close open Primary assignments. **Date only today** |

HR card: `POST /api/hr/employees`, `PUT /api/hr/employees/{id}`. Lifecycle: `POST /api/workforce/employees/{id}/transfer`, `POST /api/workforce/employees/{id}/end-employment`.

### A.2 Personnel Card today

Visible tabs: Genel → Kimlik → **Çalışma / organizasyon** → Resmî → Geçmiş.

Contract type / end / monthly hours are edited today on **Resmî → İŞKUR**. HR-04 moves **editing** to Çalışma Bilgileri.

### A.3 Existing fields vs HR-04 (no duplicates)

| Field | Owner | HR-04 |
|-------|-------|-------|
| Organization / Property display | Organization / Property via Assignment | Display only |
| DepartmentId / PositionId | Assignment | A — Transfer only after hire |
| StartDate / EndDate / Status | Employment | A — EndDate via command |
| ContractType / ContractEndDate / PartTimeMonthlyHours | Employment | A — move **edit** to this tab |
| İŞKUR / incentive / work permit | Employment | Stay Resmî |
| OfficialEmploymentProfile.* | OfficialEmploymentProfile | Stay Resmî |
| Payment IBAN | EmployeePaymentProfile | Out |
| SeniorityStartDate / TerminationReason | Employment | **New** |

---

## Appendix B — WebİK reference (not HuGu truth)

Source: Product Owner snapshot `WebİK — İnsan Kaynakları.html` (Personel Kartı `safeP`, Genel → Çalışma Durumu, Bildirge → İŞKUR, `PERSONEL_ALANLARI`). JS bundles named in discovery were not in the workspace.

WebİK has **no** tab named Çalışma Bilgileri. It overwrites department/görev in place and treats Durumu + işten çıkış as ordinary fields. **Do not copy.**

**Confirmed** on Personel Kartı / personel record: `firma`, `isGiris`, `istenCikis`, `durum` (Çalışıyor/Ayrıldı/Askıda), `sirketGiris`, `kidemGiris`, `departman`, `bolum`, `gorev`, `kademe`, `calismaGrubu`, `sozlesmeTuru`, `sozlesmeBitis`, `kismiAylikSaat`, İŞKUR/SGK exit codes and notified flags, `cikisNedeni` as a stored key, `devamKontrol`, `donemler[]`.

**REFERENCE NOT CONFIRMED** as Personel Kartı employment fields: stored manager id, personel grubu distinct from çalışma grubu, weekly hours, çalışma şekli (ATS only), contract start ≠ işe giriş, probation end date, rehire-eligibility flag.

---

## Appendix C — Gap classification (frozen outcomes)

| Class | Outcome in this freeze |
|-------|------------------------|
| **A** | Status, start, end-via-command, department/position + Transfer, property via Assignment, contract snapshot fields |
| **B** | `SeniorityStartDate`; `TerminationReason` on End Employment; contract **edit** owner = Çalışma Bilgileri |
| **C** | Assignment shape unchanged in MVP |
| **D** | `EmployeeReportingLine` and `EmploymentContract` — later, not MVP |
| **E** | Tab rename/sections; property display; optional read-only contract facts on Resmî |
| **F** | Documents, temp assignment, rehire UI, SGK exit codes, leave/shift, weekly hours, groups, grade |
| **G** | Extra PropertyId; in-place Assignment edit; manager-from-kademe; Askıda as EmploymentStatus; wage/IBAN on this tab |
