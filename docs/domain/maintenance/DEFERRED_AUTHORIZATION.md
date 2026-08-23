# Deferred: database-managed authorization

> Sprint 0.11B records the accepted direction. It does **not** implement it.

A later cross-cutting authorization sprint should let hotel administrators configure access without source-code changes. Expected concepts:

- Role
- Permission
- RolePermission
- UserRole / equivalent assignment

**Out of 0.11B:** Roles table, Permissions table, RolePermissions table, admin authorization UI, permission editor, role editor, dynamic authorization cache, Redis, permission refresh infrastructure.

Current development persona catalog remains acceptable for Development. Production authorization stays claim/policy based (`maintenance.read`, `maintenance.manage`, `maintenance.resolve`). Position names, Department names, and emails must not grant access.
