# Data ownership

> **Status:** Accepted — HR-01A. Extends Accepted Workforce ownership. Does not change HR-DOMAIN-001.

Three different boundaries must not be treated as one:

| Boundary | Meaning |
|----------|---------|
| **Aggregate** | Transactional consistency / write invariant |
| **Table** | Persistence shape; may be 1:1 with an owned entity |
| **UI composition** | Personel Card tabs and list columns |

The Personel Card may compose data from several owners in one modal.

---

## Existing Accepted chain (unchanged)

```text
Organization → Property → Department
Organization → Property → Position
Employee → Employment → Assignment → (Department, Position)
```

| Concept | Owns | Does not own |
|---------|------|----------------|
| **Employee** | Who the person is (workforce identity) | Current department as a mutable FK; payroll; leave; SGK status; login |
| **Employment** | Work relationship period and lifecycle | Daily attendance; government submission state |
| **Assignment** | Where/in which position, for a period | Permission; pay grade as authorization |
| **Department / Position** | Customer-defined structure / title | Permissions |
| **ApplicationUser** | Authentication | Employee identity |

Do **not** denormalize current Department/Position onto Employee in the write model. The card header **reads** current Primary Assignment.

---

## Personel Master extension

Recommended **practical** shape — not four new aggregate roots:

```text
Employee  (aggregate root for identity + profile writes)
  ├── EmployeeHrProfile        1:0..1   identity + contact + education summary + blood type
  ├── EmergencyContact         1:*      small collection
  ├── EmployeePhoto            1:0..1   metadata only
  └── EmployeePaymentProfile   1:0..1   later persist (HR-01C); never on operational reads

Employment  (existing aggregate / write model)
  └── later optional OriginalCompanyStartDate, SeniorityStartDate (**HR-02**)
  └── later EmploymentCompensationTerms (**HR-09**; not Employee)

Assignment  (existing)
```

### Why not four aggregate roots?

`EmployeeIdentityProfile`, `EmployeeContactProfile`, and `EmployeePaymentProfile` are warranted as **owned records / tables**, not as independent consistency boundaries. Profile fields are edited together on the Personel Card. Payment data is split **by access**, not by DDD purity: same write model, separate table, separate DTO, never joined by Technical Service / Room Operations.

Emergency contacts are a collection so the model can hold two (or a few) people without `EmergencyContact1Name` columns.

Photo bytes are **not** a domain entity. Metadata is. Storage is a port.

### Class-explosion guard

Do **not** add `EmployeeSocialProfile`, `EmployeeEducationProfile`, `EmployeeEmergencyGroup`, `EmployeeTag`, or `EmployeePortalProfile` in HR-01.

---

## Header fields — who owns what

Personel Card header (product composition):

| Header | Owner | Notes |
|--------|-------|--------|
| Photo | EmployeePhoto metadata | Not a GUID |
| Name / surname | Employee | |
| Personnel number | Employee | |
| Employment status | Employment | `Scheduled` / `Active` / `Ended` |
| Department | Current Primary Assignment | Display name from Department |
| Position | Current Primary Assignment | Display name from Position |
| Employment start | Employment.StartDate | |

Do not show GUIDs.

---

## Organization / employment fields

| Field | Owner | HR-01 |
|-------|-------|--------|
| Organization / company | Organization (display) | Yes — read-only on card |
| Property | Property (via Department) | Yes — read-only; single-property now |
| Department | Assignment → DepartmentId | Yes — mutate via Hire / Transfer, not free-text |
| Position | Assignment → PositionId | Yes — same |
| Grade / Kademe | — | Deferred |
| Working Group | — | Do not add. Revisit as EmploymentClassification later. Not Shift |
| Employment status | Employment | Yes (existing) |
| Employment start / end | Employment | Yes (existing) |
| Original company start | Employment.OriginalCompanyStartDate | **HR-02** |
| Seniority start | Employment.SeniorityStartDate | **HR-02** |
| SGK entry date | Official notification later | Later |
| Exit reason | HR-02 | Later |

Do **not** duplicate Department/Position as strings on Employee.

---

## Compensation / payment

| Data | Owner | When |
|------|-------|------|
| Current base wage, net/gross basis, wage type | `EmploymentCompensationTerms` (Compensation domain foundation, attached to Employment) | HR-09; not HR-01B |
| IBAN, optional bank name | `EmployeePaymentProfile` | Model in HR-01; persist HR-01C |
| Branch code / account number | Optional extras on payment profile | Only if IBAN is insufficient (expert question) |
| Cumulative GV / AGİ / BES / incentives | Payroll | Reject on Personel Master |
| Toplu Zam | Compensation | HR-09 |

Wage belongs to **employment terms**, not the person’s identity. IBAN belongs to the **person** (survives rehire) but is not operational workforce data.

---

## Later modules (not Personel Master)

| Later slice | May appear on card as | Must not live on Employee as source of truth |
|-------------|----------------------|-----------------------------------------------|
| HR-02 Employment entry/exit | Çalışma tab actions | Exit codes, contract type matrix |
| HR-03 Official | Resmî Bilgiler tab | SGK/KBS submission state |
| HR-04 Documents | Belgeler / Evraklar tabs | File bytes / generic DMS |
| HR-05 Assignment / promotion | Çalışma + Geçmiş | Temporary assignment UI (already conceptual) |
| HR-06–08 Leave / shift / puantaj | Optional summary tab later | Balances, rosters |
| HR-09–11 Compensation / payroll | Ücret & Ödeme | Payslips, tax cumulative |
| Education / performance / career / discipline / assets / recruitment | Separate screens; optional summaries | Scores, zimmet lists, candidates |

DISC is **not** in HuGuWeb scope.

---

## Cross-domain

Technical Service and Room Operations already use `AssignableEmployee` (`EmployeeId`, given name, family name, personnel number) for **currently employed** people.

Freeze as `OperationalEmployeeReference`. Do **not** add TCKN, IBAN, address, birth date, emergency contacts, salary, blood type, or disability.

Existing directories must not switch to Personel Card DTOs.
