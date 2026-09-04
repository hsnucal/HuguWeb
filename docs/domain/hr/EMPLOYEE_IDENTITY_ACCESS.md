# Employee identity access

> **Status:** Product/domain decision — Development seed cleanup (2026-09-04). Not an ADR. Does **not** change HR-08A or HR-08B movement domain. Does **not** authorize production auto-provisioning.

## Frozen product principle

HuGuWeb is an **employee-facing** hospitality platform.

**Every active employee should be identity-capable.**

That means an active workforce person is expected to authenticate into HuGuWeb for self-service (and later mobile), not only HR operators.

It does **not** mean every employee receives HR or admin permissions.

## Distinctions (unchanged)

| Concept | Meaning |
|---------|---------|
| **Employee** | Workforce / personnel identity |
| **ApplicationUser** | Authentication identity |
| **EmployeeAccountLink** | Connection between authenticated user and Employee |
| **Authentication** | Who signed in |
| **Authorization** | What they may do, and where (membership + roles + permissions) |

`Employee` is never `ApplicationUser`. Authentication does not imply HR authorization.

For **active** employees the target relationship is:

```text
1 Employee + 1 ApplicationUser + 1 EmployeeAccountLink
```

Inactive or ended employees may later have disabled authentication. That lifecycle is **not** implemented here.

Organizational manager (`WorkforceReportingLine`) is **not** inferred from Position title, grade, or department. Authorization roles are a separate assignment. Grade / Position hierarchy is **not** part of this decision.

## Development vs production

Development seed realizes the principle for demo personas: every **active seeded** Employee has a deterministic login, membership, baseline role, and `EmployeeAccountLink`.

Production **Hire** still must **not** create a login. Automatic provisioning is a future slice: [Identity Provisioning / Employee Access](../../product/FUTURE_SCOPE.md#identity-provisioning--employee-access).

## Baseline access

The narrowest existing role used as employee baseline is `employee-leave-self-service` (`hr.leave.request`). Operational personas keep their existing Room Operations / Technical Service roles. HR personas keep HR roles. Do not grant `hr.employee.manage` merely to make login work.

Local persona table: [Development personas](../../engineering/DEVELOPMENT_PERSONAS.md).
