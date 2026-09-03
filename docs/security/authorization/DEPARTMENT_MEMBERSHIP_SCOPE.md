# Department membership scope (AUTH-02)

> **Status:** Accepted / Completed — AUTH-02 (2026-08-30).

## Meaning

Property memberships may optionally narrow workplace access to one or more departments without introducing `AuthorizationScopeType.Department`.

```text
ApplicationUser
  └── UserMembership* (OrganizationId, PropertyId?, IsActive)
        ├── UserRoleAssignment* (RoleId)
        └── UserMembershipDepartmentScope* (DepartmentId)   // AUTH-02
```

| Condition | Effective department access |
|-----------|-----------------------------|
| Organization membership (`PropertyId` is null) | Unrestricted at membership level (not department-narrowed). Department scopes must not be configured. |
| Property membership + **zero** `UserMembershipDepartmentScope` rows | **Property-wide** — all departments in that property. |
| Property membership + one or more scope rows | Restricted to those `DepartmentId` values. |

`null` from `MembershipDepartmentAccess.GetAllowedDepartmentsAsync` means “no department filter” (property-wide or organization-wide). A non-null set means “only these departments”.

## Not `AuthorizationScopeType.Department`

AUTH-02 does **not** add a third membership scope type. Membership remains Organization | Property. Department narrowing is a **child table** under a Property membership.

- Roles stay Organization-scoped or Property-scoped.
- Permissions stay permission codes; they are not per-department.
- Multi-department access is multiple scope rows on one membership (not CSV, not one membership per department).

## Department-aware vs property-aware permissions

| Kind | Examples | Department scopes apply? |
|------|----------|--------------------------|
| Department-aware | `hr.schedule.read` / `hr.schedule.manage`, `hr.attendance.read` / `hr.attendance.manage` | Yes — endpoints resolve `AllowedDepartmentIds` via `MembershipDepartmentAccess`. |
| Property-aware | `hr.shift-definition.*`, most workforce admin | No — remain property (or organization) scoped. |

Schedule and attendance writes/reads authorize each row using the employee’s **Assignment department on that local date** (transfer-safe historical assignment), then intersect with the actor’s allowed department set.

## Admin API

`PUT /api/authorization/users/memberships/{membershipId}/department-scopes`

Body: `{ "departmentIds": ["…"] }`

- Empty array clears scopes → property-wide.
- Departments must exist and belong to the membership’s Property and Organization.
- Organization memberships reject the call (`department-scopes-require-property`).

`MembershipSummary.departmentIds` exposes the current set for admin UI.

Identity migration: `20260829210313_AddDepartmentMembershipScopes`.

## Related

- [USER_MEMBERSHIP_MODEL.md](USER_MEMBERSHIP_MODEL.md)
- Schedule wiring: [HR-06A-Shift-Schedule-Implementation-Plan.md](../../product/hr/HR-06A-Shift-Schedule-Implementation-Plan.md)
- Attendance wiring: [HR-07-PUANTAJ-DISCOVERY.md](../../product/hr/HR-07-PUANTAJ-DISCOVERY.md)
