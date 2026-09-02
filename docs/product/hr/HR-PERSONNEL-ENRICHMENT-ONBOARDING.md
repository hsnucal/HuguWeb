# HR — Personnel Enrichment & Onboarding Documents

> **Status:** Implemented / Awaiting PO Acceptance  
> **Baseline:** `4e496faf4ab6acd192232c4bd5b67cdb354b311a`  
> **Migration:** `AddPersonnelEmploymentEnrichmentAndOnboardingDocuments`

Does **not** change Accepted HR-05B leave-approval semantics. Does **not** introduce a generic DMS, workflow engine, or MediatR.

---

## 1. Shift assignment bugfix (Department manager)

### Root cause

**Permission + seed gap** (not a Department-scope algorithm bug).

`maintenance.manager@localhost` already had AUTH-02 ENG scope and leave-approver roles, but never received:

- `hr.schedule.read`
- `hr.schedule.manage`
- `hr.shift-definition.read`

`ScheduleAccess` / `MembershipDepartmentAccess` (HR-06) were already correct: Property-wide vs scoped Departments; workplace Department from Assignment covering the schedule date.

### Fix

- Productized system role `department-scheduler` with schedule read/manage + shift-definition read.
- Assigned to Maintenance Manager persona alongside `department-leave-approver` and ENG scope.
- No `DepartmentId` on `ScheduleEntry`. No auth framework change.

Manager may use Property ShiftDefinitions but may only assign for Employees in authorized Departments.

---

## 2. Certificates / Competencies

- Collection entity `EmployeeCertificate` (not CSV).
- MVP field: **Name** only (issuer/dates/expiry/upload deferred).
- Personnel Card → Resmî bilgiler → Eğitim Bilgileri → Sertifika / Yetkinlikler.
- Saved via existing Personnel Card update (replace-all like emergency contacts).

---

## 3. Work Type

- Explicit enum `WorkType`: `FullTime`, `PartTime`, `ReducedHours`, `Intern`.
- **Not** `ContractType` — independent semantics.
- Required on Employment.
- **Existing-data compatibility:** migration default **`FullTime`** (not inferred from ContractType).
- UI: required localized dropdown (Tam Zamanlı / Yarı Zamanlı / Kısmi Süreli / Stajyer).

---

## 4. Probation Period

- `ProbationPeriodMonths` nullable (`null` = Yok, `2` = 2 Ay only).
- `ProbationStartDate` required when months = 2; must be null when no probation.
- **`ProbationEndDate` derived:** `ProbationStartDate.AddMonths(2)` — not persisted.
- Example (calendar months): `01.09.2026` + 2 months = `01.11.2026` (TR DD.MM.YYYY via shared `DateField`).
- Default start for new “2 Ay” selection uses server `TimeProvider` / workforce clock (not browser as source of truth for persistence).
- Unsupported month values rejected by API.
- End date is read-only in UI with localized helper (“Otomatik hesaplanır” / equivalents).

---

## 4b. Çalışma Bilgileri nested navigation (UI IA)

Personnel Card **Çalışma Bilgileri** is a single parent section with horizontal second-level navigation (not five modules/APIs/save workflows):

| Submenu | Fields / actions |
|---------|------------------|
| İstihdam | Durum, İşe Giriş, Çalışma Şekli (`WorkType`), İşe Alım Kaynağı, Kıdeme Esas Tarih |
| Deneme Süresi | Deneme süresi, başlangıç, derived bitiş (read-only) |
| Sözleşme | `ContractType`, bitiş / part-time hours as applicable (`WorkType` ≠ `ContractType`) |
| Organizasyon | Org / Tesis / Departman / Pozisyon + existing **Transfer** workflow |
| İşten Ayrılma | End info / reason + existing **End Employment** workflow (not ordinary field edit) |

UX rules:

- Primary Personnel Card nav stays left; inside Çalışma Bilgileri only a lightweight horizontal submenu (brand `#862A51` active underline).
- Default submenu: **İstihdam**. Selection is session-only (no backend persistence).
- All five submenus share the **same** edit session — switching must not save, discard, or reset dirty form state.
- Save / Vazgeç remain global Personnel Card footer actions.
- Validation uses explicit field → submenu mapping; Save navigates to the invalid submenu and focuses the control.
- Responsive: wrap or scroll the submenu; no card horizontal overflow from labels.

No migration / domain / auth schema change for this IA decision.

## 5. Recruitment Source

- Org-owned reference catalog `RecruitmentSource` (Code/Name/IsActive/SortOrder).
- Employment stores nullable `RecruitmentSourceId`.
- Seeded defaults (idempotent): LINKEDIN, KARIYER_NET, YENIBIRIS, DIRECT_APPLICATION, **REFERRAL** (Referans).
- Behavior never inferred from display name.
- Active-only dropdown; current inactive selection remains readable and keepable on save.

Admin config UI deferred.

---

## 6. Onboarding documents — two distinct domains

### Lifecycle (PO Finding 02)

- `Employment.OnboardingStatus`: `InProgress` | `Completed`.
- **New Employment** opens as `InProgress` (checklist mutable).
- **Existing Employments** (migration backfill) are `Completed` (checklist read-only).
- Finalize via `POST /api/hr/employees/{id}/onboarding-documents/complete` — no reopen in this sprint.
- Checklist mutations after `Completed` return Problem Details `onboarding-completed`.
- Creating Personnel continues into the same card on **İşe Giriş Evrakları** (edit mode) without hunting the list.
- Printing / opening DOCX is **not** an onboarding mutation.

### Left — Mandatory checklist

- Catalog: `OnboardingDocumentRequirement` (org-owned).
- Status: `EmploymentOnboardingDocumentStatus` (**Employment-scoped**; fresh checklist per Employment).
- Checkbox means: “İK tarafından teslim alındı / kontrol edildi.”
- **No upload** in this sprint.
- Audit: CompletedAtUtc + CompletedByUserId on check; cleared on uncheck.
- Narrow API — does not dirty the main Personnel Card form.
- Existing personnel UI: read-only status list (no checkboxes).

Default catalog (configurable policy, not a universal legal list):

| Code | Name |
|------|------|
| ID_COPY | Kimlik Fotokopisi |
| RESIDENCE | İkametgâh Belgesi |
| CRIMINAL_RECORD | Adli Sicil Belgesi |
| DIPLOMA | Diploma / Mezuniyet Belgesi |
| HEALTH_REPORT | Sağlık Raporu |
| PHOTO | Vesikalık Fotoğraf |
| BANK_IBAN | Banka / IBAN Bilgisi |

### Right — Printable matbu templates

- Narrow `HrDocumentTemplate` (Category `Onboarding`) with optional `TemplateAssetPath` (application-owned relative id only).
- Backend HTML preview renderer with **allow-listed** placeholders only (convenience).
- **OVERTIME-CONSENT** authoritative body is project-owned DOCX: `Templates/Onboarding/Taslak.docx` (imported from PO `Taslak.docx`; embedded resource).
- DOCX generation: `GET /api/hr/employees/{id}/document-templates/{templateId}/docx` returns `application/vnd.openxmlformats-officedocument.wordprocessingml.document`.
- Placeholders in DOCX: `{{Employment.StartDate}}`, `{{Employee.FullName}}`. Signature remains blank/manual.
- Frontend action: **Word'de Aç / Yazdır** downloads generated DOCX (browser/OS opens Word via association — no Office Interop / `ms-word:` hacks).
- No arbitrary path from client; path traversal rejected.
- Frontend never injects personnel values into official form body.
- HTML batch print remains for non-DOCX templates only; DOCX templates use per-document download.

#### Placeholder catalog

`{{Employee.FullName}}`, `{{Employee.GivenName}}`, `{{Employee.FamilyName}}`, `{{Employee.PersonnelNumber}}`, `{{Employee.BirthDate}}`, `{{Employment.StartDate}}`, `{{Assignment.DepartmentName}}`, `{{Assignment.PositionName}}`, `{{Organization.Name}}`, `{{Property.Name}}`, `{{CurrentDate}}`

Unknown placeholders → validation error. No reflection traversal, expressions, scripts, or SQL.

Dates: Property/localization context; TR `dd.MM.yyyy`; `CurrentDate` from TimeProvider.

#### OVERTIME-CONSENT seed

- Code: `OVERTIME-CONSENT`
- Name: Fazla Çalışma Muvafakat Belgesi
- Version: 1
- TemplateAssetPath: `Templates/Onboarding/Taslak.docx`
- HTML preview content is metadata/placeholder convenience aligned to the same allow-listed fields; DOCX is authoritative.

---

## 10. PO Finding 02 addenda

### AR-GE Project Code removed

- Removed from domain, API, UI, Excel import catalog, localization, and current sprint migration (`DropColumn` on `EmployeeHrProfiles.ArgeProjectCode`).
- Intentional data loss for that legacy column in local/dev DBs.

### Global DateField

- Shared `DateField` is the only editable date control.
- Default: calendar icon button (`type="button"`) opens native `<input type="date">` picker via `showPicker()` / click fallback.
- Visible value remains typed **DD.MM.YYYY**.
- Read-only/disabled dates (e.g. Deneme Bitiş) do **not** expose an interactive picker.

### Migration

Still **exactly one** uncommitted sprint migration relative to baseline:

`20260831211853_AddPersonnelEmploymentEnrichmentAndOnboardingDocuments`

Includes: enrichment tables/columns, onboarding lifecycle `OnboardingStatus`, `TemplateAssetPath`, and AR-GE column drop.

---

## 11. PO Finding 03 — Department lookup + active Personnel list regression

### Root causes

- **Bug A (empty Department dropdown):** `ActiveWorkforcePage` loaded personnel, departments, and positions in one `Promise.all`. Property-scoped department/position APIs failed when session property context was missing, which rejected the whole bundle and left `departments` at `[]`. The load effect did not re-run when `propertyId` changed after property selection. Create mode showed `—` for Organization/Property because it only read `card`, which is null before first save.
- **Bug B (empty active Personnel list):** Same coupled `Promise.all` failure cleared `directory` even when `/api/hr/employees` would have succeeded. **Not** caused by onboarding lifecycle, WorkType, or `EmployeeAccountLink` filters.

### Fixes

- Decouple personnel list load (`loadPersonnelEmployees`) from property structure load (`loadPropertyStructure`).
- Reload structure when `user.propertyId` changes.
- Session user now includes `organizationName`; create-mode Organization/Property display uses session workplace labels.
- Explicit empty states when property selection is required or no accessible departments exist.
- Backend regression tests for directory visibility and department property guard.

### Runtime verification (dev)

- `hr.manager@localhost` + Ankara property: `organizationName` = Demo Hotel Group, property = Ankara Hotel (from `accessibleProperties`), **6** active departments returned.
- `/api/hr/employees` requires sprint migration columns on `Employments` (e.g. `OnboardingStatus`). Local `huguweb_dev` showed partial schema drift (`WorkType` present, `OnboardingStatus` absent) → 500 until migration history/DB are aligned. **Not** the F03 list regression root cause (frontend `Promise.all` coupling).

Status remains: **Implemented / Awaiting PO Acceptance**

---

## 12. PO Finding 04 — Onboarding create-time editable, existing read-only

### Final PO rule

İşe Giriş Evrakları is editable/actionable only during new Personnel onboarding (`Employment.OnboardingStatus == InProgress`). After explicit finalization it becomes historical read-only information. Matbu evrak preview/generate/print is closed for completed onboarding.

### Root causes / gaps

- Onboarding tab hidden during Personel Ekle (`mode.type === 'edit'` gate).
- Matbu preview/DOCX endpoints had no `OnboardingStatus` guard (completed employments could generate).
- Checklist draft before first save was not persisted on create.
- `initialTab: 'onboarding'` after create did not switch tab (state initializer only).

### Fixes

- Onboarding tab visible in create and edit; create mode uses local draft checklist (all unchecked initially).
- `POST /api/hr/employees/{id}/onboarding-documents/sync` persists checklist on first save.
- `GET /api/hr/onboarding-document-requirements` supplies catalog before Employee exists.
- DTO: `canEditChecklist`, `canGenerateDocuments` (from `OnboardingStatus`).
- Backend rejects checklist mutation with `onboarding-documents-read-only`; DOCX/preview with `onboarding-document-generation-closed`.
- Existing Personnel: read-only checklist indicators; Matbu section informational only (no actions).
- Hire still opens `InProgress`; only explicit finalize sets `Completed`.

Status remains: **Implemented / Awaiting PO Acceptance**

---

## 7. Security

- Checklist / templates / render: existing HR employee tenant + workplace guards.
- Render accepts IDs only; backend resolves personnel data.
- Cross-Organization template access blocked.
- HTML content sanitized (no script/onload/onclick/javascript:/iframe).

---

## 8. Deferred

- Certificate issuer / dates / expiry / upload
- Recruitment Source admin config UI
- Checklist catalog admin UI
- Template editor UI
- Employee file uploads
- Signed scanned document archive
- Digital signature
- Generated document persistence / history
- Full template version-history UI

---

## 9. Localization

TR / EN / RU for Work Type, probation, recruitment source, certificates, onboarding tab, checklist, matbu actions, and Çalışma Bilgileri submenu labels (`İstihdam` / `Deneme Süresi` / `Sözleşme` / `Organizasyon` / `İşten Ayrılma` and EN/RU equivalents).
