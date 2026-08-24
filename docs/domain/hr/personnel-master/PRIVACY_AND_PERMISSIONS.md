# Privacy, classification, and permissions

> **Status:** Accepted — HR-01A. Policy direction only. No DB-managed roles. No personas implemented.

## Why this sprint tightens security

Personel Master introduces PII (contact, birth date) and highly sensitive identifiers (TCKN, address, later IBAN). Sprint 0.7B/0.8 `workforce.read` / `workforce.manage` is too broad for that data.

---

## Classification

| Class | Meaning | Examples |
|-------|---------|----------|
| **NORMAL HR DATA** | Needed for operational workforce | Name, personnel number, department, position, employment status, start date |
| **SENSITIVE HR DATA** | Personal but commonly needed by HR | Mobile, email, birth date, gender, nationality, education level, blood type |
| **HIGHLY SENSITIVE / RESTRICTED** | Identity, location, money, medical/legal, emergency | TCKN / YKN / passport number, home address, notification address, IBAN / bank, emergency contacts, disability |

Photo is PII (normal-to-sensitive). Treat list thumbnails as allowed for `hr.employee.read`; do not put photos on operational assignable-employee APIs unless a later slice justifies it. **HR-01 freeze:** operational directories stay without photo.

---

## API / view-model boundaries

Do **not** use one unrestricted Employee DTO for every domain.

| Surface | Allowed |
|---------|---------|
| `OperationalEmployeeReference` | EmployeeId, DisplayName (given + family), PersonnelNumber. Optional later: Department/Position **names** only if a floor workflow proves need. **None of the sensitive classes.** |
| Personel List (default) | Normal HR data + photo for HR users |
| Personel Card (non-sensitive) | Normal + Sensitive, requires `hr.employee.read` |
| Personel Card (restricted) | Highly sensitive, requires `hr.employee.sensitive.read` |
| Payment | Payment profile DTO only; never embedded in list or operational APIs |
| Export | Same field-level rules as the UI the user can see |

Existing Technical Service / Room Operations `AssignableEmployee` already matches this direction (`EmployeeId`, names, `PersonnelNumber`). **Do not expand it.**

---

## Permission split (forward-compatible)

Authorization remains **permission / policy based** (ADR-008). Position and Department names still never grant access.

| Permission | Purpose |
|------------|---------|
| `workforce.read` | Operational employee references; Personel **list of normal columns**; departments/positions read |
| `workforce.manage` | Maintain departments/positions; Hire / Transfer / End Employment (existing lifecycle) |
| `hr.employee.read` | Personel Card non-highly-sensitive HR fields |
| `hr.employee.manage` | Create/update Personel Master profile (non-bank); photo replace |
| `hr.employee.sensitive.read` | TCKN, addresses, emergency contacts, later IBAN/bank, disability |
| `hr.employee.sensitive.manage` | **Later** — write highly sensitive fields when dual-control is justified (especially bank and national-id corrections) |

### `sensitive.manage` now or later?

**Later.** For HR-01B, `hr.employee.manage` may write identity/contact including TCKN if the user also has `hr.employee.sensitive.read` (or we require both manage + sensitive.read to display and save those fields). Introduce `hr.employee.sensitive.manage` when bank data and identity-number corrections need a stricter deny case.

Do not implement a permission-admin UI or persist roles in the database in HR-01B beyond the existing claim/policy pattern.

### Navigation

Personel menu may remain visible with `workforce.read` (today). Opening the full Personel Card sensitive tabs requires `hr.employee.read`. Floor users with only `workforce.read` keep today’s operational list/detail **without** the new profile fields until HR-01B hides them.

HR-01B must not accidentally return TCKN on the existing `GET /api/workforce/employees/{id}` used by the current detail page. Add a **new** HR profile contract or explicitly omit restricted fields from the old payload.

---

## Personas (direction only — do not implement)

Persona names are **not** runtime roles.

| Future persona | Likely permission bundle | Not |
|----------------|--------------------------|-----|
| `hr.specialist` | `workforce.read`, `workforce.manage`, `hr.employee.read`, `hr.employee.manage`, `hr.employee.sensitive.read` | Official submission, payroll calculate |
| `hr.manager` | Specialist plus later compensation / official permissions; `hr.employee.sensitive.manage` when it exists | Position-name checks |

`DEVELOPMENT_PERSONAS.md` currently has `hr.manager@localhost` with only `workforce.*`. Do **not** add `hr.specialist@localhost` in HR-01A. Adjust development claims in HR-01B when the new permissions exist.

---

## Security rules (freeze)

- Least privilege; no single “HR sees everything” DTO.
- Sensitive fields not returned by default.
- No secrets, TCKN, or IBAN in logs.
- No TCKN/IBAN in URLs or filenames shown to unauthorized users.
- No sensitive identifiers in client-side analytics.
- Masking (e.g. TCKN `***********`) is appropriate on card/list when the user lacks sensitive.read but knows a field exists — prefer **omit** over tease.
- Export is permissioned per column.
- Audit of sensitive **access** is later (not a generic enterprise audit framework).
- Do not design field-level encryption in this sprint unless platform policy already requires it (it does not).

---

## Data minimization invariant

Technical Service and Room Operations **must not** receive expanded HR profile DTOs.

If a future floor workflow needs department on an assignable picker, add **names only** through the operational reference, never the Personel Card payload.
