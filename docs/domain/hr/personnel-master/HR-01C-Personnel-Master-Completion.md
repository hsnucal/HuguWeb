# HR-01C — Personnel Master Completion

> **Status:** Accepted — Product Owner + CTO approved (2026-08-27).
>
> **Product Owner decision (pre-acceptance):** Bulk photo import was removed before HR-01C acceptance.
> Employee photos are managed individually from Personnel Card (add / replace / remove).

## Scope delivered

- Excel export (filtered list + visible columns + mandatory identity fields)
- Excel import (HuGu template, preview, confirm, row-level validation)
- Employee payment profile (IBAN + optional bank name, sensitive)
- Personnel profile change history (HR-owned, masked sensitive values)
- ERP account visibility + create-user entry point from Personnel Card
- Personnel list / card integration

**Not in HR-01C:** Bulk photo import.

## Export design

- Format: `.xlsx` via **ClosedXML** (backend only)
- Permissions: `hr.employee.read`; sensitive columns require `hr.employee.sensitive.read`
- Filters: search, department, position, status, employment start range (same as list)
- Columns: **visible column picker selection + always include** personnel number, given name, family name when safe
- Formula injection: values starting with `=`, `+`, `-`, `@` prefixed with `'`
- Header row: HuGu brand fill `#862A51`, white bold text, frozen, AutoFilter on every exported column
- Column widths: content-aware (`AdjustToContents` plus longest-value fallback), clamped approximately 10–50

## Import design

- HuGu template with hidden `_meta` sheet for stable column ids (`hugu-personnel-import-v2`)
- Template header: HuGu `#862A51` fill, white bold text, AutoFilter, frozen row, `*` on required labels
- Yardım + Referans sheets; enum dropdowns for small stable lists; department/position codes from the active Property
- Matching: **PersonnelNumber** for updates; blank sicil → auto-generate on create; existing sicil cannot be changed
- Blank cells on update = **no change**
- Parser, preview validation, confirm, and persistence share `PersonnelImportColumnCatalog`
- Sensitive columns (identity, address, emergency contact, IBAN) require `hr.employee.sensitive.read`
- Payment import writes `EmployeePaymentProfile` (IBAN + optional bank). No salary/payroll columns
- Preview table stays compact; field-level errors appear in Hata / Açıklama
- Blank cells on update = **no change**
- Preview required; confirm imports **only when zero invalid rows**
- Confirmed import runs inside a **single workforce transaction**; any row failure rolls back the entire import (no partial writes)
- Preview token bound to actor user, organization/property context, and sensitive-permission scope; 30-minute TTL
- Single-node implementation; distributed preview storage is deferred until deployment topology requires it
- Reuses `HireEmployeeWithProfileUseCase` / `UpdateEmployeeHrProfileUseCase`
- Limits: 5000 rows, 5 MB workbook
- Import history: `PersonnelImportRun` (summary only, no cell storage)
- UI: compact HuGu workspace dialog (`min(880px, 100vw-48px)` wide; `height: auto`; `max-height: min(82vh, 760px)`). Content-sized before preview; internal table scroll after preview. Header + helper text, sticky footer inside the panel, existing WorkspaceDialog backdrop.

## Photos

Bulk photo import removed before HR-01C acceptance.
Employee photos are managed individually from Personnel Card.

## Payment profile

- Entity: `EmployeePaymentProfile` (1:0..1 per employee)
- Not on Employee core; not on operational DTOs
- IBAN normalized uppercase, mod-97 validation, no salary/payroll fields

## Profile history

- Entity: `PersonnelProfileChange`
- Separate from employment/assignment history in UI (`Geçmiş` tab sections)
- TCKN/IBAN masked in stored/display values where appropriate

## ERP account

- Card shows linked user email or “Hesap yok”
- Authorized admins link to Settings → Users with `employeeId` query param
- No auto-provision on hire; no role from department/position

## Database

Migration: `AddPersonnelMasterHr01C`

- `EmployeePaymentProfiles`
- `PersonnelProfileChanges`
- `PersonnelImportRuns`

No schema change for bulk-photo removal: that flow reused existing `EmployeePhoto` storage.

## API routes

| Method | Route |
|--------|-------|
| GET | `/api/hr/employees/export` |
| GET | `/api/hr/employees/import/template` |
| POST | `/api/hr/employees/import/preview` |
| POST | `/api/hr/employees/import/confirm` |
| GET | `/api/hr/employees/{id}/profile-history` |
| GET | `/api/hr/employees/{id}/erp-account` |
| PUT | `/api/hr/employees/{id}/payment-profile` |

## Manual acceptance checklist

A — Excel Export: Personel → Excele aktar. Header `#862A51` / white bold; filter arrows; frozen header; column widths fit longest practical content; data correct.

B — Excel Import Modal: Personel → Excel'den aktar. Compact HuGu dialog, backdrop covers list, no giant blank area, Şablonu indir, Dosya seç, drag/drop, footer inside modal.

C — Excel Import Preview: summary chips, visible table headers, aligned rows, internal scroll if needed, footer remains visible.

D — Bulk Photo: **no** Toplu Fotoğraf action. Personnel Card individual photo management still works.

E — Regression: Personel Card Fotoğraf ekle / değiştir / kaldır still works.

## Non-goals (unchanged)

No payroll, leave, shift, SGK/KBS/İŞKUR submission, or new business domains.
