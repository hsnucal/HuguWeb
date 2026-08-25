# Audit

> **Status:** Accepted — ARCH-01 (2026-08-25).

Two concepts. Do not replace domain histories with a generic JSON audit table.

## A. Security administration

`AuthorizationAuditRecord`:

- `OccurredAtUtc`
- `ActorUserId`, `ActorOrganizationId`, `ActorPropertyId`
- `Action` (stable code: `membership-created`, `role-assigned`, …)
- `SubjectUserId`, `MembershipId`, `RoleId`, `PermissionCode`
- `Details` (non-secret; never passwords or tokens)

Enough to answer: who changed whose access, for which organization/property, what kind of change, when.

## B. Business domain history

Owned by each domain (`RoomReadinessHistory`, `MaintenanceIssueHistory`, …). Same convention: actor, organization/property when applicable, UTC timestamp, business action/code.
