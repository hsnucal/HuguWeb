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

`IWorkplaceContext` remains the module-facing workplace. For HTTP it is `RequestWorkplaceContext`: Organization from membership claims; operational Property from **`ActiveWorkplaceResolution`** (shared with `/me` and `ICurrentTenantContext` / `ActorContext`).

Resolution order is: property-scoped membership Property always wins (cookie cannot escalate); otherwise the explicit `HuGuWeb.ActiveProperty` selection from `ActivePropertyCookie.ResolveSelection()` (same-request `HttpContext.Items` after `Bind`, then request cookie); then the sign-in ticket claim. `ActivePropertyCookie.Bind` writes the response cookie **and** the same-request selection so `RefreshSignIn` after `PUT /api/auth/property` does not rebuild the principal from the old request cookie.

Authenticated users **do not** fall back to `Workforce:PropertyId` or to the first accessible Property. Unauthorized Property selection is rejected. `/me` and `IWorkplaceContext` must resolve the same Property for a request. Movement APIs do **not** special-case this; they consume the shared workplace context.

## Actor context

`ActorContext` is request-scoped, immutable, owned by the API boundary:

- `UserId`
- `EmployeeId?` from `EmployeeAccountLink` (claim `employee_id`)
- `OrganizationId`
- `PropertyId?`
- `MembershipId`
- `ScopeType`
- `OccurredAtUtc` from `TimeProvider`

`Employee` is not `ApplicationUser`. Not every user is an Employee (operator accounts such as `dev@localhost` and `hr.corporate@localhost` may remain unlinked). Product intent: every **active** Employee should be identity-capable via `EmployeeAccountLink`. Development seed follows that for demo employees. Production hire still does not auto-provision a login.

Do not scatter `HttpContext` claim lookups through domain use cases. Domain must not reference `HttpContext`.

## Active Property

Stored in cookie `HuGuWeb.ActiveProperty`, **not** on `ApplicationUser`.

`PUT /api/auth/property` validates membership access, calls `ActivePropertyCookie.Bind`, and reissues the sign-in cookie so permission/scope claims match the **new** selection on that same request.
