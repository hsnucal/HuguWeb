# Permissions and privacy

> **Status:** Accepted — HR-03A. Extends Accepted Personel Master privacy. No DB-managed roles. No personas implemented by this record.

## Classification

| Field / record | Class | Why |
|----------------|-------|-----|
| Belge türü, tabi kanun, sigorta kolu | **SENSITIVE HR DATA** | Statutory employment classification; HR-restricted; not floor work |
| Meslek kodu | **SENSITIVE HR DATA** | Official occupation; not medical; not TCKN-level |
| SGK İşyeri / `SgkWorkplaceRegistrationId` on the profile | **SENSITIVE HR DATA** (selection) plus **Organization confidential** (the registration number itself) | Which workplace applies is HR classification; the sicil number is property configuration |
| SGK işyeri sicil (Property configuration) | **Organization confidential** (treat as sensitive configuration, not Personel Master PII) | Workplace registration; not an employee identifier |
| Görev kodu (not in 03B) | Would be Sensitive HR **if** later validated | Discovery only; not collected in 03B |
| VKN / credentials (out of slice) | HighlySensitive / Secret | Not collected in 03B |
| Disability, parent names (out of slice) | HighlySensitive | Unchanged Personel Master mapping |
| SGK notified flags (out of slice) | Notification — not master | |

Meslek kodu is **not** HighlySensitive in the TCKN/address/IBAN sense. It still must not appear on operational APIs.

---

## Permission recommendation (first slice)

Keep the Personel Master split. Do **not** add `hr.official.read` / `hr.official.manage` merely for this slice.

No conflict found with Accepted HR-01 permissions: official classification is HR employee data already behind `hr.employee.*`. A new family would be warranted when **submission** exists (who may send to SGK vs who may edit master codes) or when payroll and HR must be split. That is not 03B.

| Permission | Official data |
|------------|----------------|
| `hr.employee.read` | Read Bildirge Kodları on Personel Card (including selected SGK İşyeri) |
| `hr.employee.manage` | Write OfficialEmploymentProfile (including selecting an existing registration) |
| `hr.employee.sensitive.read` | **Not required** for meslek/belge/kanun/kolu/SGK İşyeri. Still required for TCKN/address/emergency (unchanged) |
| `hr.employee.sensitive.manage` | Still later (bank / identity corrections) |
| `workforce.read` | No official codes. List stays normal columns |
| `workforce.manage` | Maintain `SgkWorkplaceRegistration` on Property (configuration); existing Hire/Transfer/End |

**Property SGK registration management is a configuration concern**, not a Personel Card concern and not a reason to invent `hr.official.*`. First implementation surface is a small HR/workforce configuration edit (`workforce.manage`). No Property admin product exists today.

Do **not** implement DB-managed authorization.

`sensitive.read` is not used as the gate for meslek kodu — otherwise floor-adjacent HR assistants with read-but-not-sensitive could not complete bildirgeler. If a hotel later wants meslek hidden from some HR readers, that is a new permission — not invented now.

---

## Cross-domain minimization (freeze)

`OperationalEmployeeReference` / `AssignableEmployee` remains:

`EmployeeId`, `GivenName`, `FamilyName`, `PersonnelNumber`

**Do not add:**

- workplace registration / SGK işyeri sicil / `SgkWorkplaceRegistrationId`
- document type (belge türü)
- applicable law (tabi kanun)
- insurance branch (sigorta kolu)
- occupation code (meslek kodu)
- görev kodu
- SGK no
- disability

Room Operations and Technical Service must not consume Personel Card official DTOs. They remain unaware of these fields.

Personel List default columns do not include official codes. Column picker may offer them only to `hr.employee.read`, never via `workforce.read` alone.

---

## Personas (direction only)

`hr.manager@localhost` already has `hr.employee.read/manage/sensitive.read` plus `workforce.*`. 03B can use that bundle without a new persona.

Do not add `hr.specialist`. Do not persist roles in the database.
