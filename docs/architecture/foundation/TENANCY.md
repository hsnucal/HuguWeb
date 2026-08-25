# Tenancy

> **Status:** Accepted — ARCH-01 (2026-08-25).

## Database strategy

**One database, one schema, shared tables.** Tenant isolation uses `OrganizationId`, `PropertyId`, and relational ownership.

Do **not** create table-per-hotel, schema-per-hotel, or database-per-hotel in the current architecture.

Later, a large dedicated customer or regional shard may move to another database **without changing domain identities** (stable Guids).

## Organization vs Property

| Request | `PropertyId` |
|---------|----------------|
| Organization-scoped (Corporate HR directory) | May be null |
| Property-scoped (Room Operations, Technical Service, hire/transfer, departments) | **Must** be explicit |

`ICurrentTenantContext` / `IWorkplaceContext` live at the API/application boundary. Domain entities do not hold HTTP tenant context.

## No silent Property inference

A Property-scoped domain must **never** infer:

- configured pilot Property
- default hotel
- first Property
- oldest membership

from an Organization-wide membership.

If the user has **only** property memberships and exactly one accessible Property, that Property may be auto-selected at sign-in.

If the user has Organization-wide membership, **do not** auto-select a Property. Return `property-context-required` (400) for operational commands until the session cookie `HuGuWeb.ActiveProperty` is set via `PUT /api/auth/property`.

## Query policy

Frontend filters are UX only. Backend always scopes.

Prefer explicit store methods (`ListRoomsAsync(propertyId)`, host `EmployeeTenantGuard`) over EF global query filters. Do not introduce `IRepository<T>`.

Unknown cross-property ids return **404** with stable codes (`employee-not-found`, `room-not-found`, `issue-not-found`).

## Unique identifiers

| Identifier | Scope |
|------------|--------|
| Room number | `UNIQUE(PropertyId, Number)` |
| Personnel number | `UNIQUE(OrganizationId, PersonnelNumber)` |
| Role code | `UNIQUE(OrganizationId, Code)` |
| Department / Position codes | Property-owned rows; names are **not** globally unique (existing rule) |

## Indexes added with AUTH-01 / ARCH-01

Identity:

- `UserMemberships`: `UserId`, `OrganizationId`, filtered `PropertyId`, plus existing unique org-wide / property pairs
- `UserRoleAssignments`: `MembershipId+RoleId` unique (covers MembershipId)
- `RolePermissions`: PK `(RoleId, PermissionCode)`
- `AuthorizationAuditRecords`: `OccurredAtUtc`, `ActorUserId`, `ActorOrganizationId`

Workforce: existing `OrganizationId` / `PropertyId` indexes; `Properties.TimeZoneId` column (not a tenant index).
