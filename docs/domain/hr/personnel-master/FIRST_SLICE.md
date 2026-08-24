# First implementation slice

> **Status:** Accepted freeze for **HR-01B** (next) and **HR-01C** (after). HR-01A does **not** start either.

Personel Master **model** is larger than the first production slice. Do not make HR-01B enormous.

---

## Quality bar (must stay true after 01B)

1. Employee owns identity (id, org, sicil, names) plus owned HR profile — not payroll/leave/SGK.
2. Employment owns relationship dates/status. Optional seniority/company-start wait for HR-02.
3. Assignment owns department/position history.
4. Profile owns identity/contact/photo (payment later).
5. Sensitive vs highly sensitive is enforced in DTOs.
6. Operational modules see only `OperationalEmployeeReference`.
7. Personel Card tabs 1–3 and 8 work; 4–7 hidden.
8. Personel List stays scannable with default columns.
9. Import/export are **not** in 01B but rules are frozen.
10. This document is the 01B contract.

---

## HR-01B — frozen production slice

### In

- Employee HR profile foundation (`EmployeeHrProfile`).
- Large Personel Card overlay: **create / edit / read**, same card.
- Identity: `NationalIdentityScheme` / `NationalIdentityNumber`, Nationality, Gender, BirthDate, BirthPlace, MaritalStatus, optional BloodType.
- Contact: MobilePhone, optional HomePhone, Email, ResidenceAddress, City, District, optional Notification/StayAddress.
- `EmergencyContact` collection (Name, Relationship, Phone, IsPrimary; more than one record).
- Optional `EducationLevel` summary.
- `EmployeePhoto` metadata + provider-neutral storage object (not base64 on Employee); replace/delete permissioned; file validation.
- Organization/employment summary on the card (header reads current Primary Assignment).
- Geçmiş composition using **existing** Employment/Assignment history.
- Unsaved-changes guard.
- Create mode still performs Hire (Employee + Employment + Primary Assignment) in one transaction; profile saved in the same operation when present.
- Çalışma tab **invokes** existing Transfer / End Employment; does not invent new lifecycle.
- Permission split: `hr.employee.read`, `hr.employee.manage`, `hr.employee.sensitive.read`. Existing `workforce.read` / `workforce.manage` remain. Sensitive-field redaction. Do not persist highly sensitive fields on old workforce DTOs. Do not expand AssignableEmployee.
- Personel List: photo, name, sicil, department, position, start, status. Search (no TCKN by default), department, position, status, start-date range filters.
- Permission-aware column picker; **local** UI preference.
- TR / EN / RU for new UI strings.
- Tests and data-minimized APIs.
- Validations: sicil (existing), optional TCKN format when scheme=Tckn, email format, phone normalization, birth-date sanity, employment period (existing).

### Out of HR-01B

- Excel import / export
- Bulk photo
- Bank / IBAN (`EmployeePaymentProfile`)
- Wage / current salary / salary history / Toplu Zam
- Profile change-history table
- `hr.employee.sensitive.manage`
- Grade
- WorkingGroup
- EmploymentClassification
- Disability
- Parent names
- SGK / KBS / İŞKUR, government credentials/clients, notification tables
- Documents / onboarding checklists / Belgeler / Evraklar
- Leave, shift, attendance, payroll
- Performance, training, career, discipline, asset assignments, recruitment, portal
- Identity booklet serial numbers / obsolete booklet fields
- OriginalCompanyStartDate / SeniorityStartDate (HR-02)
- Physical delete / “Personeli Sil”
- DB-managed roles / new development personas
- Technical Service / Room Operations DTO expansion
- Spreadsheet libraries, cloud photo vendors, encryption architecture
- Fake/empty future tabs

### Must still prove

- Foreign employee can be saved **without** TCKN.
- Housekeeping/technical assignable lists still return only id + names + sicil.
- Former staff remain listed; ending employment deletes nothing.
- Transfer/end still work from the card without a second status model.
- `workforce.read` cannot add TCKN / home address / emergency contacts via the column picker.

---

## HR-01C — frozen later direction (do not implement now)

| Item | Notes |
|------|--------|
| Excel export | Authorized fields only; permission-filtered; optionally respects current filters |
| Excel import | Template → Upload → Mapping → Validation preview → Confirm → Result report. No silent partial import. Input data only |
| Bulk photo | ZIP/files → PersonnelNumber filename match → preview unmatched/duplicate/invalid → confirm. Optional TCKN/YKN match only when authorized |
| EmployeePaymentProfile | IBAN + optional BankName. Highly sensitive. Not on operational APIs |
| Narrow Personel Master profile history | Sicil correction, identity-number correction, legal name change, contact change, bank change. Not enterprise audit |

HR-01C is still Personel Master, not Compensation.

---

## Conceptual HR dependency map

```text
HR-00  Organization & Workforce Foundation     Accepted (0.7–0.8)
HR-01B Personel Master                         next implementation
HR-01C Bulk / profile extensions               Excel, bulk photo, IBAN, narrow history
HR-02  Employment / entry–exit                 contract type, exit reason, rehire, optional seniority/company-start, EmploymentClassification?
HR-03  Official / government                   SGK, KBS, İŞKUR; parent names; disability; official codes
HR-04  Documents                               Belgeler / Evraklar / checklists
HR-05  Assignment / promotion                  temp assignment UI, later Grade
HR-06  Leave
HR-07  Shift
HR-08  Attendance / puantaj
HR-09  Compensation                            wage history, Toplu Zam, EmploymentCompensationTerms
HR-10  Payroll inputs
HR-11  Payroll                                 likely integrate calculation
…      Education, performance, career, discipline, assets, recruitment, portal
```

Do **not** skip remaining HR slices to open another department (Product Owner rule).

HR-04 can parallel HR-03. HR-05 extends Assignment already in the foundation. HR-09 can follow 01C payment profile; payroll still needs attendance. HR-03 must not start before 01B identity exists.

---

## Closed by this acceptance

Former discovery questions, now decided:

| Question | Decision |
|----------|----------|
| Foreign identity scheme | `Tckn` / `Ykn` / `Passport` / `Other`; number optional |
| Stay vs residence address | Optional Notification/StayAddress in HR-01B |
| Wage on Personel Card day one | No. HR-09 |
| Disability at hire | HR-03 |
| Working Group | Do not add. Revisit as EmploymentClassification |
| Grade at hire | Deferred. Position is enough |
| IBAN vs full bank | HR-01C: IBAN + optional BankName |
| Blood type | Optional HR-01B; not list/operational/auth |
| Parent names | HR-03 |

Remaining (HR-02, not Personel Master): same-day department/position correction on işe giriş day.
