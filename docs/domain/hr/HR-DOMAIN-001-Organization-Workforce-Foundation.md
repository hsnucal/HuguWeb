# HR-DOMAIN-001: Organization & Workforce Foundation

## Status

**Accepted**

Product Owner + CTO approved baseline (2026-08-21). This is a **domain** decision record, not an architecture ADR. It authorizes Sprint 0.7B to implement [FIRST_SLICE.md](FIRST_SLICE.md) only. It does not authorize government integrations, Identity linking, or deferred HR products.

Approved product/domain direction is **not** a validated universal hotel truth. The model may evolve when hotel HR experts, operational users, pilot hotels, or official integration specifications provide stronger evidence. Avoid bureaucracy around minor later adjustments.

---

## Context

HuGuWeb is a hospitality-first ERP / PMS for independent mid-size hotels ([Target Customer](../../product/TARGET_CUSTOMER.md)). Application foundation exists. **No hotel business domain is implemented yet.** Sprint 0.7B will be the first real business-domain implementation.

Full Human Resources / Payroll is **not** an approved MVP module ([MVP Candidates](../../product/MVP_CANDIDATES.md)). Operational workflows will still need to know **who works here, in which department, in which position**, without a second staff spreadsheet.

Constraints this model must not contradict:

- Modular monolith; modules added only when approved ([ADR-001](../../architecture/adr/ADR-001-Architecture-Style.md))
- Identity at Host; modules consume a user id ([ADR-007](../../architecture/adr/ADR-007-Authentication-Strategy.md))
- Permission-based authorization; no department/position-name checks ([ADR-008](../../architecture/adr/ADR-008-Authorization-Strategy.md))
- Explicit Property concept; no tenant / hotel-group implementation ([TECHNOLOGY_DECISIONS.md](../../architecture/TECHNOLOGY_DECISIONS.md))
- No event sourcing, brokers, or multi-tenancy infrastructure
- UI language is a user preference ([LOCALIZATION.md](../../product/LOCALIZATION.md))

---

## Boundary

**In:** Organization, Property, Department, Position, Employee, Employment, Assignment, Personnel Number, employment lifecycle, assignment history, future government-integration **readiness**, localization strategy.

**Out:** Leave, attendance, shift scheduling, puantaj, payroll, overtime, training, recruitment, performance, employee documents, accident records, transport, accommodation, access cards / passcards, employee mobile, organization chart, manager hierarchy, full HR, full payroll, multi-property UI, tenant infrastructure, Hotel Group.

The slice is named **Organization & Workforce Foundation**, not “İK Modülü.”

Do not create placeholder implementation concepts for deferred scope.

---

## Core concepts

```text
Organization → Property → Department
Organization → Property → Position
Employee → Employment → Assignment → (Department, Position)
```

`Employee ⇢ ApplicationUser` is **not** part of Sprint 0.7B. Hiring must not create a login.

Product discussion uses Turkish terms. Internal identifiers stay English. See [DOMAIN_MODEL.md](DOMAIN_MODEL.md).

---

## Decision

We will model and (in Sprint 0.7B) implement the first HR-related domain as follows:

1. **Employee is not User is not Role is not Permission is not Position.** Hiring an Employee does not create a HuGuWeb login. Account provisioning is a separate future Identity workflow. ApplicationUser is not required. Sprint 0.7B must not implement Employee/User linking. Position and Department must not grant permissions.
2. **No separate Person aggregate.** Employee holds core name identity. Rehire is a later new Employment on the same Employee (rehire UI is out of 0.7B).
3. **Organization ≠ Property.** Persist both as thin records. Organization is the employer / company boundary. Property is the physical operating hotel and conceptually belongs to Organization. First implementation is single-property. Do not introduce Tenant, Hotel Group, or multi-tenancy infrastructure. Do not collapse Organization and Property.
4. **Department and Position are customer-defined data**, not enums. First implementation uses a **flat** department list. Do not add `ParentDepartmentId` yet. Architecture must not unnecessarily prevent hierarchy later. No `IsManagerial` on Position.
5. **Employment** is the work relationship: `Scheduled` | `Active` | `Ended`. `Scheduled` is required because a person may be entered before their start date. Employment state is **not** attendance, shift, leave, or sickness.
6. **Assignment** records department, position, period, and kind (`Primary` | `Temporary`). Primary is the home assignment. Temporary is joker/coverage and stays in the conceptual model. Sprint 0.7B must not expose temporary-assignment UI/API. Primary assignments must not overlap.
7. **Transfer date semantics:** if a transfer starts on date D, the previous Primary Assignment ends on D−1 and the new Primary Assignment starts on D. No time-of-day assignment boundaries. History is retained.
8. **PersonnelNumber** is unique within Organization, manually assigned in the first implementation, never reused after employment ends, and is **not** the database primary key. Do not implement automatic numbering now.
9. **Manager relationships are deferred.** Do not implement `ReportsToEmployeeId` in Sprint 0.7B. No org chart, matrix management, or supervisor hierarchy.
10. **Minimize personal data:** given name, family name, personnel number. Sensitive HR attributes wait for a later slice. Do not add speculative government-specific Employee fields.
11. **Aggregates are not a giant Employee graph.** The conceptual chain Employee → Employment → Assignment does not mean one aggregate eagerly owns lifetime history. Sprint 0.7B chooses the smallest boundaries needed to implement Hire, Transfer, and End Employment safely. Do not freeze EF navigation, eager-loading, or repository shape in this record.
12. **Conceptual events** `EmployeeHired` and `EmploymentEnded` (and assignment change) are documented so future official-notification adapters can consume business facts without coupling the core to SGK or KBS. No message broker, outbox, worker, or scheduler in 0.7A or 0.7B for this readiness.
13. **Official Turkish government notifications** (SGK işe giriş / işten ayrılış, Emniyet KBS, Jandarma KBS, and later legal obligations) are a required **future** capability. Sprint 0.7B must not implement them. A HuGuWeb workforce commit and an external government submission are **not** the same transaction. Government failure must not erase valid internal history. Government-system status must not become Employment status.
14. **First production slice** is [FIRST_SLICE.md](FIRST_SLICE.md): maintain Departments and Positions; Hire; Transfer; End Employment; list active workforce; seed one Organization and one Property.

---

## Key decisions

| Topic | Choice |
|-------|--------|
| Slice name | Organization & Workforce Foundation, not full HR |
| Person | Not a separate aggregate |
| User | Not required; linking deferred past 0.7B |
| Organization | Thin employer; persist; one seeded instance |
| Property | Explicit operating hotel; persist; one seeded instance |
| Hotel Group / Tenant | Deferred; not implemented |
| Department hierarchy | Deferred; flat list first |
| Department / Position | Customer-defined data; not enums; not i18n keys |
| Position / Department → auth | Forbidden as enforcement |
| Employment states | Scheduled / Active / Ended |
| Assignment history | Required |
| Temporary assignment | In model; not 0.7B UI/API |
| Transfer dates | Previous Primary ends D−1; new Primary starts D |
| Manager | Deferred; no `ReportsToEmployeeId` in 0.7B |
| PersonnelNumber | Unique per Organization; manual; never reused; not PK |
| Cross-property Employee | Future compatibility only; not implemented |
| Customer names | One working-language string |
| Delete | End-date / deactivate; ending employment is not deletion |
| Government notifications | Readiness only; adapters outside core; no 0.7B implementation |
| Module in code | Not created in 0.7A; 0.7B implements the first slice |

---

## Closed questions

Former discovery questions, now decided:

| Question | Decision |
|----------|----------|
| PersonnelNumber uniqueness / reuse / numbering | Unique within Organization; never reused; manual first; auto-numbering later |
| Organization vs Property | Persist both; do not collapse |
| Same-day transfer convention | Previous Primary ends D−1; new starts D |
| Is `Scheduled` needed? | Yes |
| Direct manager in first persist? | No. Deferred. |
| Parent departments in year one? | No. Flat list. |
| Hire attaching a user? | No. Linking is a later Identity workflow. |
| Keep `AssignmentKind`? | Yes, conceptually. 0.7B creates Primary only. |
| Single working name vs multilingual master data? | One stored working name for 0.7B |
| Same person across future properties? | Same Employee identity should remain possible later; do not implement now |

---

## Rejected alternatives

| Alternative | Why |
|-------------|-----|
| Complete HR module now | Out of MVP direction; payroll/leave/recruitment explosion |
| Employee = User | Floor staff without login; Identity bugs would couple to HR |
| Position or department as Role | ADR-008; hotels rename structure without changing permissions |
| Hard-coded department/position enums | Hotels differ; names are customer data |
| `Employee.DepartmentId` only | Cannot transfer with history |
| Shared Person with Guest/candidates | Wrong bounded context |
| Employment status including Leave/Sick | Mixes relationship with daily availability |
| Event sourcing / brokers / outbox for “readiness” | Premature infrastructure |
| Distributed transaction with SGK / KBS | External downtime must not roll back valid HuGuWeb history |
| Placeholder `ISgkService` / `IKbsService` | No approved integration slice yet |
| Full group / tenant / legal-entity tree | Speculative; target is one independent hotel |
| Giant Employee aggregate loading all history | Convenience is not a consistency boundary |
| Turkish type names | Domain identity must be locale-stable |

---

## Risks

| Risk | Mitigation |
|------|------------|
| Experts later need hierarchy, suspension, or contractors | Add after evidence; do not invent now |
| Assignment model feels heavy for one hotel | 0.7B includes Transfer so history is proven, not flattened |
| Optional future UserId becomes required | 0.7B Hire has no Identity dependency |
| Government fields creep onto Employee | Keep adapters outside the core; no speculative SGK/KBS columns |
| Treating Accepted as unchangeable hotel law | Status is approved direction, not universal truth |

---

## Implementation recommendation

1. Implement only [FIRST_SLICE.md](FIRST_SLICE.md) in Sprint 0.7B.
2. Keep Identity, localization resources, and auth policies unchanged except new coarse permissions for the new screens.
3. Add tests for [INVARIANTS.md](INVARIANTS.md).
4. Do not create Leave/Payroll projects, government clients, event bus, Organization/Property admin UI, or Employee/User linking.

---

## Date

2026-08-21

---

## Related Documents

- [README.md](README.md)
- [DOMAIN_MODEL.md](DOMAIN_MODEL.md)
- [ORGANIZATION_MODEL.md](ORGANIZATION_MODEL.md)
- [WORKFORCE_MODEL.md](WORKFORCE_MODEL.md)
- [INVARIANTS.md](INVARIANTS.md)
- [FIRST_SLICE.md](FIRST_SLICE.md)
- [ADR-001](../../architecture/adr/ADR-001-Architecture-Style.md)
- [ADR-007](../../architecture/adr/ADR-007-Authentication-Strategy.md)
- [ADR-008](../../architecture/adr/ADR-008-Authorization-Strategy.md)
- [PRODUCT_PRINCIPLES.md](../../product/PRODUCT_PRINCIPLES.md)
- [ENGINEERING_PRINCIPLES.md](../../engineering/ENGINEERING_PRINCIPLES.md)
