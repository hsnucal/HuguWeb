# Request context

> **Status:** Accepted — ARCH-01 (2026-08-25).

## Tenant context

`ICurrentTenantContext` (API):

- `UserId`
- `OrganizationId`
- `MembershipId`
- `PropertyId?`
- `ScopeType`
- `HasOrganization` / `HasProperty`

`IWorkplaceContext` remains the module-facing workplace. For HTTP it is `RequestWorkplaceContext` (claims). Authenticated users **do not** fall back to `Workforce:PropertyId`.

## Actor context

`ActorContext` is request-scoped, immutable, owned by the API boundary:

- `UserId`
- `EmployeeId?` from `EmployeeAccountLink` (claim `employee_id`)
- `OrganizationId`
- `PropertyId?`
- `MembershipId`
- `ScopeType`
- `OccurredAtUtc` from `TimeProvider`

`Employee` is not `ApplicationUser`. Not every user is an Employee; not every Employee has a User.

Do not scatter `HttpContext` claim lookups through domain use cases. Domain must not reference `HttpContext`.

## Active Property

Stored in cookie `HuGuWeb.ActiveProperty`, **not** on `ApplicationUser`.

`PUT /api/auth/property` validates membership access, refreshes the cookie, and reissues the sign-in cookie so permission/scope claims match.
