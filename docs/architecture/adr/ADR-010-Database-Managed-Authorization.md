# ADR-010: Database-Managed Membership Authorization

## Status

Accepted

Accepted by Product Owner and CTO on 2026-08-25 after AUTH-01 + ARCH-01 database and runtime verification.

---

## Context

ADR-007 authenticates with ASP.NET Core Identity inside HuGuWeb. ADR-008 authorizes with permission claims and ASP.NET policies; roles are permission bundles, not the unit of enforcement.

Development bootstrap stored permission claims directly on Identity users (`AspNetUserClaims`) from a hardcoded persona catalog (`hr.manager@localhost` and similar). That was acceptable for local screens. It is not a production ERP authorization model.

HuGuWeb already persists Organization and Property as separate concepts. Employees are not ApplicationUsers. Runtime access must follow the authenticated user, an active organization/property membership, assigned roles, and the union of those roles’ permissions.

---

## Problem

How should HuGuWeb persist and evaluate organization/property-scoped roles and permissions without:

- hard-coding emails, Position names, or Department names
- turning ASP.NET Identity roles into a multi-tenant IAM product
- coupling Workforce / Room Operations / Technical Service domain modules to Identity types
- building a generic ABAC engine

---

## Decision

We will:

1. **Keep ASP.NET Core Identity for authentication only** (users, passwords, cookies, lockout, security stamp).
2. **Layer HuGu authorization entities over Identity** rather than extending `IdentityRole` for hotel-scoped assignment.
3. **Source permission claims from the database at sign-in** (and on security-stamp refresh). Existing permission policies stay.
4. **Treat permissions as a code-owned catalogue.** Role composition (which permissions a named role has) is database-owned.
5. **Scope access through `UserMembership`** (user + organization + optional property). Do not store a global `PropertyId` on `ApplicationUser`.
6. **Keep `Employee` distinct from `ApplicationUser`.** Optional `EmployeeAccountLink` connects them when an ERP login exists.

This ADR does **not** introduce Excel as a runtime identity store. Excel, if added later, is an import channel into the same use cases.

---

## Alternatives Considered

| Alternative | Outcome | Reason |
|-------------|---------|--------|
| A — Extend ASP.NET Identity roles (`IdentityRole` + user-role rows) | Rejected | Identity roles are global to the user store. They do not express Organization vs Property membership, multiple memberships, or membership-scoped assignment. Stretching them couples hotel IAM to Identity internals. |
| B — HuGu authorization entities layered over Identity | Accepted | Membership, role, and permission assignment are hotel-domain configuration. Identity remains the password/cookie boundary. Domain modules still consume permission claims and workplace ids. |
| Permission claims remain the only persisted assignment | Rejected for production | Cannot be administered per hotel without code or user-claim editing. No membership scope. |
| Full ABAC / external policy engine | Rejected | Out of ADR-008 and AUTH-01 scope. |

---

## Consequences

### Positive

- Runtime authorization no longer special-cases development emails.
- Hotels can compose roles without a code change.
- Existing policies (`workforce.read`, `hr.employee.manage`, …) keep working.
- Future OIDC/SSO (ADR-007) still only replaces authentication; membership stays in HuGuWeb.

### Negative

- A second role concept exists beside unused `AspNetRoles` tables. Those Identity tables remain unused for HuGu ERP roles.
- Cross-context references (`UserId` string, `OrganizationId` / `PropertyId` Guid) are logical, not cross-DbContext foreign keys — consistent with Room Operations already referencing `PropertyId` without owning Property.

---

## Risks

| Risk | Mitigation |
|------|------------|
| Stale cookie permissions after an admin change | Admin writes bump affected users’ security stamps. `SecurityStampValidator.ValidationInterval` is **1 minute** (not `TimeSpan.Zero`). `/api/auth/session` reloads the DB snapshot and refreshes the cookie if permissions/property drifted. Maximum expected stale-permission window for cookie-only policy checks: **1 minute**. |
| Identity types leaking into domain modules | Authorization entities and Identity live in the Host. Architecture tests forbid module → Identity references. |
| Permission explosion | Catalogue grows only when a distinct deny case exists (ADR-008). |
| Treating Position/Department as access | Architecture tests and authorization code paths must not read those names. |

---

## Revisit Conditions

- A hotel group requires inherited organization→property permission sets beyond membership union.
- Relationship rules (“only the assigned attendant”) dominate simple permissions.
- Identity is replaced by an external IdP that must *assert* groups; HuGu membership remains the authorization source.

---

## Date

2026-08-25

---

## Related Documents

- [ADR-007 Authentication Strategy](ADR-007-Authentication-Strategy.md)
- [ADR-008 Authorization Strategy](ADR-008-Authorization-Strategy.md)
- [AUTH-DOMAIN-001](../../security/authorization/AUTH-DOMAIN-001.md)
- [ARCH-FOUNDATION-001](../foundation/ARCH-FOUNDATION-001.md) (Accepted — ARCH-01)
- [TENANCY](../foundation/TENANCY.md)
- [REQUEST_CONTEXT](../foundation/REQUEST_CONTEXT.md)
- [USER_MEMBERSHIP_MODEL](../../security/authorization/USER_MEMBERSHIP_MODEL.md)
- [ROLE_PERMISSION_MODEL](../../security/authorization/ROLE_PERMISSION_MODEL.md)
