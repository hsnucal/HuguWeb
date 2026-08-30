# User membership model

> **Status:** Accepted — AUTH-01 (2026-08-25).

## Meaning

A membership means: **this user may operate inside this organization, and optionally this property.**

```text
ApplicationUser
  └── UserMembership* (OrganizationId, PropertyId?, IsActive)
        ├── UserRoleAssignment* (RoleId)
        └── UserMembershipDepartmentScope* (DepartmentId)  // AUTH-02 — see DEPARTMENT_MEMBERSHIP_SCOPE.md
```

A user may belong to multiple properties, and later multiple organizations. `ApplicationUser` itself has no `PropertyId`.

Property memberships may optionally carry **DepartmentScopes** (zero rows = property-wide). This is not `AuthorizationScopeType.Department`. Details: [DEPARTMENT_MEMBERSHIP_SCOPE.md](DEPARTMENT_MEMBERSHIP_SCOPE.md).

## Shape

| Field | Role |
|-------|------|
| Id | Stable identity (Guid) |
| UserId | Identity user id (string, logical reference) |
| OrganizationId | Employer / company boundary (Guid, logical reference to Workforce Organization) |
| PropertyId | Nullable. Null = organization-wide membership. Set = that property only. |
| IsActive | Deactivated membership grants no permissions and no workplace. |
| CreatedAtUtc | Audit |

Uniqueness:

- one active-or-inactive row per `(UserId, OrganizationId)` where `PropertyId` is null
- one row per `(UserId, OrganizationId, PropertyId)` where `PropertyId` is set

## Employee link

Optional, separate from membership:

| Field | Role |
|-------|------|
| UserId | Unique. One login ↔ at most one employee. |
| EmployeeId | Unique. One employee ↔ at most one login. |
| CreatedAtUtc | Audit |

Not every employee has an ERP account. Hire does not create a login. Former employees keep the Employee row; the membership or account may be disabled independently.

Do not put `ApplicationUserId` on `Employee`.

## Active context

At sign-in the host selects an **active membership** and may set an **active Property cookie**:

| Active memberships | Behavior |
|--------------------|----------|
| 0 | Sign-in succeeds. Session has no permissions and no workplace claims. |
| 1 property membership | Auto-select that membership and that Property. |
| Organization-wide (alone or with properties) | Select the organization membership. **Do not** auto-select a Property. |
| >1 property memberships, no org-wide | No auto Property; client must `PUT /api/auth/property`. |

A Property switcher is in ARCH-01 (header selector). Active Property is **not** stored on `ApplicationUser`.

Organization-wide memberships do **not** use a configured pilot Property for Room Operations or Technical Service. Those APIs require an explicit Property (`property-context-required`). HR for organization-wide memberships remains organization-scoped (see [TENANT_SCOPE](TENANT_SCOPE.md)).

## Lifecycle

| Action | Effect |
|--------|--------|
| Membership created | User may receive roles in that scope. Audit row. |
| Membership deactivated | Immediately excluded from effective permissions. Security stamp refreshed. Audit row. |
| Account lockout / disable | Identity concern. Distinct from membership and from Employee termination. |
| Employee ended | Workforce concern. Does not by itself delete the login. |

## Cross-database references

`UserId` lives in the Identity model. `OrganizationId` / `PropertyId` / `EmployeeId` are Guids owned by Workforce. There is **no** cross-DbContext foreign key — the same pattern Room Operations already uses for `PropertyId`.
