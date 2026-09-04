# Workforce Model

> **Status:** Accepted — Product Owner + CTO approved baseline. **Evidence:** E0–E1.

## Employee

Employee is the workforce identity of a person employed (now or in the past) by the Organization.

- Technical id is the database primary key.
- `PersonnelNumber` (Sicil No) is the business identifier. It is **not** the primary key.
- Unique within **Organization**.
- Manually assigned in the first implementation. Do not implement automatic numbering now.
- Must **not** be reused after employment ends.
- Core identity: given name, family name, personnel number.
- `ApplicationUser` is **not** the Employee. Hiring in production must **not** create a login.
- Product intent: every **active** employee should be identity-capable (`ApplicationUser` + `EmployeeAccountLink`). See [EMPLOYEE_IDENTITY_ACCESS.md](EMPLOYEE_IDENTITY_ACCESS.md). Development seed links active demo employees. Production provisioning is a separate future slice.

Employee records are **not deleted** when someone leaves.

Long-term compatibility: the same Employee should eventually be able to work across multiple Properties of the same Organization without a duplicate identity. Do not implement cross-property assignments now.

---

## Employment

Employment is the **work relationship**, not presence on a given day, not shift state, not leave, and not government-notification status.

```text
Employee
  └── Employment (period + lifecycle)
        └── Assignment (department + position + period + kind)
```

This is a conceptual chain. It is not an instruction to load lifetime history into one aggregate. See [DOMAIN_MODEL.md](DOMAIN_MODEL.md#aggregate-boundaries).

### Why a collection of employments

Rehire is a real hotel pattern. One Employee may have many Employment records over time. Rehire UI is **out of Sprint 0.7B**.

**Invariant:** at most one non-ended employment at a time (`Scheduled` or `Active`).

### Lifecycle

| State (internal) | Product label direction (TR) | Meaning |
|------------------|------------------------------|---------|
| `Scheduled` | Planlandı / Başlamadı | Entered before the employment start date |
| `Active` | Aktif | Relationship currently in force |
| `Ended` | Sona erdi | Relationship finished |

`Scheduled` is required. `Draft` is a UI concern, not a persisted business state.

**Out of this enum:** on leave, sick, day off, overtime, no-show, SGK/KBS submission state.

Fields: start date, optional end date, status. No payroll law, contract-type matrix, or government codes on Employment.

Ended employment: set end date and status `Ended`. **Do not delete.**

Successful Hire / End Employment may later create official notification *obligations* (SGK, KBS). Those obligations are not part of the Employment record and are not implemented in 0.7B.

---

## Assignment

Do **not** store current department/position only as FKs on Employee. That cannot preserve transfer history.

Assignment records department, position, start date, optional end date, kind, and the Employment it belongs to.

### AssignmentKind

| Kind | Meaning | Sprint 0.7B |
|------|---------|-------------|
| `Primary` | Normal / home assignment. At most one non-ended primary per employment. Must not overlap. | **Yes** — Hire and Transfer create/close Primary rows |
| `Temporary` | Temporary / joker coverage | Conceptual only. **No** UI/API |

### Transfer date semantics

If a transfer starts on date D:

- previous Primary Assignment ends on **D−1**
- new Primary Assignment starts on **D**

No time-of-day assignment boundaries. Primary assignments must not overlap. History is retained: do not overwrite previous Department/Position and do not delete the previous Assignment.

### Current workforce read

“Who currently works here, in which Department, in which Position?” = employees with non-ended employment and their current Primary Assignment. Do not denormalize current department onto Employee in the write model.

---

## Manager / reporting line

`WorkforceReportingLine` is the effective-dated Employment-to-Employment manager relationship.

Direct manager eligibility uses Position **OrganizationalLevel** (vertical catalogue) and **CanManageEmployees** (independent eligibility flag). Required manager level is the next configured active level above the subordinate as of EffectiveDate. OrganizationalLevel is not authorization. CanManageEmployees is not authorization. Do not infer a manager from title, role, or missing supervisors at the next level.

---

## Personal information

| Class | This slice |
|-------|------------|
| Given name, family name, personnel number | **In** |
| Employment period and status | **In** (on Employment) |
| Assignment, department, position | **In** (on Assignment) |
| `UserId` / invitation | **Out of 0.7B** |
| TCKN, bank, address, tax, emergency contact, photo, SGK numbers, KBS-specific fields | **Out** of this Accepted foundation. The later slice is **Accepted** as [Personel Master (HR-01A)](personnel-master/README.md). This document’s Accepted status is unchanged. |

Do not collect government identifiers “for future integration” **in this foundation**. Official adapters remain outside the core. Personel Master (Accepted) may collect **prerequisite identity/contact fields** without implementing SGK/KBS.

---

## Authorization

Frozen: **Employee ≠ User ≠ Role ≠ Permission ≠ Position.**

The workforce domain must not grant permissions because a position or department name contains “Supervisor” or “İnsan Kaynakları.”

---

## Future consumers

This foundation should make the following possible **without** embedding them:

| Later area | Needs from this foundation |
|------------|----------------------------|
| Kat Hizmetleri / Teknik Servis | Active employees, department, position |
| Shift planning, attendance, leave, overtime | Employee + employment identity; **separate** status models |
| Payroll calculation | Identity + employment period; **integrate**, do not build payroll |
| Official notifications (SGK, Police KBS, Jandarma KBS) | `EmployeeHired` / `EmploymentEnded` facts; adapters outside core |
| Access cards, training, transport, accommodation | Employee id; separate products |
| Employee mobile | Later Identity link; not 0.7B |

If a consumer needs “who is on duty today,” it must not overload `Employment.Status`.

---

## Privacy and audit

- Collect only what this slice needs.
- Past assignments remain business records; access is still permissioned.
- Do not write sensitive identifiers into technical logs when those fields eventually exist.

Business actions likely to need an audit trail later: Hire, Transfer, End Employment, personnel-number correction. Distinct from log files.
