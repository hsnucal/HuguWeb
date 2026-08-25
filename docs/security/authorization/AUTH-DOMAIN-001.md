# AUTH-DOMAIN-001 — Database-managed users, roles, and permissions

> **Status:** Accepted — AUTH-01 (2026-08-25).
> **Evidence:** Development personas were bootstrap only. Production ERP access is membership + role + permission.

## Intent

Replace hardcoded development personas as the *authorization architecture* with a property/organization-scoped ERP model.

Users sign in as `ApplicationUser`. What they may see and do depends on:

1. authenticated Identity user
2. active `UserMembership` (organization, optional property)
3. roles assigned to that membership
4. union of those roles’ permission codes

Not on:

- hardcoded email or username
- Position name
- Department name
- frontend-only checks

## Frozen distinctions

| Concept | Meaning |
|---------|---------|
| **Authentication** | Identity (ADR-007). Who the user is. |
| **Authorization** | HuGu membership/roles/permissions (ADR-008, ADR-010). What they may do, where. |
| **Employee** | Workforce person. Not a login. |
| **ApplicationUser** | ERP login account. |
| **Permission** | Stable application capability (`hr.employee.manage`). Code-owned. |
| **Role** | Named bundle of permissions. Database-owned. Never checked by name at runtime. |
| **Excel** | Future import/provisioning channel. Never a runtime identity store. |

## Host ownership

Authorization entities, claim factory, and admin APIs live in the API host.

Workforce, Room Operations, and Technical Service **do not** reference Identity types. They receive:

- permission checks at the HTTP policy boundary
- `IWorkplaceContext` organization/property ids for data filtering

## Invariants

1. `Employee` primary key is never the Identity user id.
2. `ApplicationUser` does not store a global `PropertyId`.
3. Runtime authorization does not branch on email, Position, or Department.
4. Development persona emails may appear **only** in Development seed/catalog code.
5. Menu hiding is UX. APIs remain authoritative.
6. SGK, KBS, payroll, Room Readiness, and Technical Service lifecycle are out of AUTH-01.

## Related

- [USER_MEMBERSHIP_MODEL](USER_MEMBERSHIP_MODEL.md)
- [ROLE_PERMISSION_MODEL](ROLE_PERMISSION_MODEL.md)
- [TENANT_SCOPE](TENANT_SCOPE.md)
- [DEVELOPMENT_PERSONAS](DEVELOPMENT_PERSONAS.md)
- [ADMIN_UX](ADMIN_UX.md)
- [EXCEL_PROVISIONING](EXCEL_PROVISIONING.md)
- [LOCALIZATION_ARCHITECTURE](LOCALIZATION_ARCHITECTURE.md)
- [FIRST_SLICE](FIRST_SLICE.md)
- [ADR-010](../../architecture/adr/ADR-010-Database-Managed-Authorization.md)
