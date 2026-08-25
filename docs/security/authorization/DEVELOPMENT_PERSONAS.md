# Development personas (seed data)

> **Status:** Accepted — AUTH-01 (2026-08-25). Supersedes the *architecture* in [engineering/DEVELOPMENT_PERSONAS.md](../../engineering/DEVELOPMENT_PERSONAS.md); emails and passwords stay.

## Decision

Development emails remain **test data**. They are not authorization logic.

The Development seeder:

1. Creates/finds the same Identity users as today (`dev@localhost`, `hr.manager@localhost`, …).
2. Does **not** reset passwords. User Secrets stay in force.
3. Creates/finds database roles (system templates) on the seeded organization.
4. Syncs `RolePermission` rows to the template catalogue.
5. Creates/finds membership: property membership when the persona has a Property; **organization-wide** when `PropertyId` is null (`dev@localhost`, `hr.corporate@localhost`).
6. Assigns the matching role (role `OrganizationId` and `ScopeType` must match membership).
7. Removes ERP `permission` claims from `AspNetUserClaims` so runtime claims come from membership.

Development organization: **Demo Hotel Group**. Properties: **Ankara Hotel**, **Antalya Hotel**.

Runtime code paths (policies, endpoints, sidebar, calculators) must not contain those emails. Only `DevelopmentPersonaCatalog` / `DevelopmentUserSeeder` / tests that assert seed mapping may.

## Mapping

| Email | Role code |
|-------|-----------|
| `dev@localhost` (or `DevelopmentUser:Email`) | `development-superuser` |
| `hr.manager@localhost` | `hr-manager` (Ankara membership) |
| `hr.antalya@localhost` | `hr-manager` (Antalya membership) |
| `hr.corporate@localhost` | `hr-corporate` (organization-wide) |
| `roomops.attendant@localhost` | `room-attendant` |
| `roomops.inspector@localhost` | `room-inspector` |
| `roomops.manager@localhost` | `room-operations-manager` |
| `maintenance.technician@localhost` | `maintenance-technician` |
| `maintenance.manager@localhost` | `maintenance-manager` |

Permission bundles match the previous persona table so local UX does not regress.

## After AUTH-01

Signing out and in still refreshes the cookie. Role changes bump the security stamp. Cookie policy claims may be up to **one minute** stale; `/api/auth/session` (including window focus) reloads from the database.
