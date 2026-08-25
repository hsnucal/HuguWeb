# Tenant / workplace scope

> **Status:** Accepted — AUTH-01 + ARCH-01 (2026-08-25).

## What AUTH-01 / ARCH-01 isolate

HuGuWeb is not a SaaS tenant platform (ADR-001 / ADR-004). Isolation is **organization and property** on membership and on domain rows (`OrganizationId` / `PropertyId`). **One database, one schema, shared tables.**

## Workplace source

For authenticated HTTP requests the host supplies a **request-scoped** workplace from membership claims and the `HuGuWeb.ActiveProperty` cookie:

1. Organization from the active membership.
2. Property only when the membership is property-scoped **or** the user explicitly selected a Property they can access.
3. Organization-wide membership does **not** receive a configured/pilot/first Property.
4. Seeders may still read `Workforce:OrganizationId` / `PropertyId` from configuration. Authenticated users without membership do not gain that hotel’s data.

Property-scoped domains (Room Operations, Technical Service, hire/transfer, departments) return `property-context-required` until `PropertyId` is explicit.

## Data access (server-side)

Menu hiding is not security.

| Module | Filter |
|--------|--------|
| Workforce departments/positions | `PropertyId` required |
| Employee identity | `OrganizationId` |
| HR directory/card | Host `EmployeeTenantGuard`: property membership → assignment property; organization-wide → whole organization. Cross-scope ids → `employee-not-found` (404). |
| Room Operations / Technical Service | `PropertyId` on rooms/issues. Wrong hotel id → not found. |

## Organization vs property roles

| Membership | HR employees | Rooms / maintenance |
|------------|--------------|---------------------|
| Property (Hotel X) | Employees whose current or last primary assignment is at Hotel X | Hotel X only |
| Organization (`PropertyId` null) | All employees in the organization | **No** automatic Property. User must select. Until then, operational APIs return `property-context-required`. |

Permissions answer **WHAT**. Membership/context answers **WHERE**.

## Active Property

Cookie session, not `ApplicationUser`. `PUT /api/auth/property`. Auto-select only when the user has no organization-wide membership and exactly one accessible Property.
