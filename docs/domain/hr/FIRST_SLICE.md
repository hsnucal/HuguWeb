# First Implementation Slice — Sprint 0.7B

> **Status:** Frozen direction for Sprint **0.7B**. Sprint 0.7A does not implement this.

Sprint 0.7B is the first real HuGuWeb business-domain implementation. It proves Organization & Workforce Foundation — not a complete İK portal, not payroll, and not government integration.

---

## What 0.7B must prove

```text
Organization → Property → Department → Position
Employee → Employment → Primary Assignment
```

Bootstrap: **one Organization**, **one Property**. No Organization admin UI. No Property admin UI.

---

## Required workflows

1. **Maintain Departments** — create, rename, deactivate. Customer-defined names. Flat list. Not enums.
2. **Maintain Positions** — create, rename, deactivate. Belongs to a department. Customer-defined names. Not enums. Not permissions.
3. **Hire Employee** — one business operation, not three CRUD calls.
4. **Transfer Employee** — proves historical Primary Assignment behavior.
5. **End Employment** — closes the relationship; deletes nothing.
6. **List Active Workforce** — who currently works here, in which Department, in which Position.

### Hire

Minimum input:

- Given Name
- Family Name
- Personnel Number
- Employment Start Date
- Department
- Position

Hire conceptually creates **Employee + Employment + Primary Assignment** in one transaction.

If start date is in the future, Employment is `Scheduled`; otherwise `Active` (implementation may treat “today” as Active). Do not require ApplicationUser. Do not create a login. Do not link Identity.

Successful Hire may **later** trigger official notification obligations (SGK işe giriş, applicable KBS). Sprint 0.7B must **not** submit them.

### Transfer

Input: employee, new department, new position, effective date D.

- previous Primary Assignment ends on **D−1**
- new Primary Assignment starts on **D**
- history remains queryable

Transfer must not overwrite previous Department/Position or delete the previous Assignment.

Government notification implications, if any, are **not** part of Sprint 0.7B.

### End Employment

- never deletes Employee, Employment, or Assignment history
- closes active Employment
- closes the applicable active Primary Assignment
- retains historical workforce information

`EmploymentEnded` may **later** produce obligations (SGK işten ayrılış, applicable KBS). Sprint 0.7B must **not** submit them.

### Active workforce query

Operational list: currently employed people, current Department, current Position.

Do not turn this into BI, a dashboard framework, analytics, or reporting infrastructure.

---

## Explicitly out of Sprint 0.7B

### HR / workforce

Leave, attendance, shift scheduling, puantaj, payroll, overtime, training, recruitment, performance, employee documents, accident records, transport, accommodation, access cards, passcards, employee mobile, organization chart, manager hierarchy, temporary-assignment UI/API, rehire UI, user invitation / account provisioning, full HR, payroll calculation.

### Organization

Multi-property UI, tenant infrastructure, Hotel Group, full legal company master data, Organization admin UI, Property admin UI.

### Government integration

SGK integration, Police KBS integration, Jandarma KBS integration, government notification persistence, automatic government submissions, government credential management, background submission workers, retry infrastructure, placeholder services (`ISgkService`, `IKbsService`, `IGovernmentIntegrationService`).

### Infrastructure not to introduce “for readiness”

Message broker, RabbitMQ, Kafka, event bus, outbox, background worker, scheduler.

Do not create placeholder classes for deferred features.

---

## UI direction for 0.7B

Product labels in Turkish by default; UI strings go through existing `tr` / `en` / `ru` localization. Department/position **names** are stored data.

```text
Departmanlar
Pozisyonlar
Personel (aktif)     Sicil / Ad / Departman / Pozisyon
Personel detayı      iş ilişkisi + görevlendirme geçmişi
```

No org chart, permission admin, or personal-file screens.

---

## Conceptual persistence preview

**No SQL. No EF. No migration in 0.7A.** Illustrative only.

| Table | Role |
|-------|------|
| Organizations | Thin employer; personnel-number uniqueness scope |
| Properties | Workplace; department scope |
| Departments | Configurable structure |
| Positions | Configurable titles |
| Employees | Workforce identity (no `UserId`, no `ReportsToEmployeeId` in 0.7B) |
| Employments | Relationship + period + status |
| Assignments | History; kind `Primary` \| `Temporary` (0.7B writes Primary only) |

Identity tables stay in the Identity context. Do not add Employee/User FK in this slice.

Likely columns (not a schema):

- Departments: id, propertyId, code?, name, isActive
- Positions: id, departmentId, code?, name, isActive
- Employees: id, organizationId, personnelNumber, givenName, familyName
- Employments: id, employeeId, startDate, endDate?, status
- Assignments: id, employmentId, departmentId, positionId, startDate, endDate?, kind

Created/modified actor and time when persistence exists. No generic `IsDeleted`.

Do not freeze eager-loading or “Employee owns all history” as the persistence model. Load what Hire / Transfer / End Employment / list-active need.

---

## API preview

**Do not create these endpoints in 0.7A.** Prefer business intent over table-CRUD.

| Intent | Candidate |
|--------|-----------|
| Maintain departments | `GET/POST /api/departments`, `PATCH /api/departments/{id}` |
| Maintain positions | `GET/POST /api/positions`, `PATCH /api/positions/{id}` |
| Hire | `POST /api/employees/hire` |
| Transfer | `POST /api/employees/{id}/transfer` |
| End employment | `POST /api/employees/{id}/end-employment` |
| Active workforce | `GET /api/workforce` |
| Employee detail + history | `GET /api/employees/{id}` |

Do not expose Identity register/login through this surface.

Permissions later: coarse checks (maintain structure vs view workforce vs hire/transfer/end). Never `if (positionName == "Supervisor")`.

---

## Testing model (when 0.7B code exists)

Test invariants, not getters:

- Reject inverted employment or assignment periods
- Reject assignment outside employment period / without a valid employment
- Reject new assignment on `Ended` employment
- Reject second non-ended employment
- Transfer ends previous Primary on D−1 and starts new Primary on D without overlap
- Reject new assignment to inactive department or position
- Reject duplicate or reused personnel number within Organization
- End employment deletes nothing
- Hire does not require or create ApplicationUser

Persistence tests belong in PostgreSQL integration tests. Do not test EF mapping as unit tests.

---

## 0.7B must still not

- Start additional HR modules (Leave, Payroll, Attendance, …)
- Implement SGK, Police KBS, or Jandarma KBS
- Add government notification tables or speculative government fields
- Introduce brokers, outbox, or background jobs for future notifications
- Change authentication beyond coarse permissions for the new screens
- Collect TCKN, bank, address, or government registration numbers
- Implement Employee/User linking, manager hierarchy, or temporary-assignment workflows
