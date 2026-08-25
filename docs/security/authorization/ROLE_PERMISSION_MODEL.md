# Role and permission model

> **Status:** Accepted — AUTH-01 (2026-08-25).

## Permissions (code)

Permissions are stable application capabilities. They are **not** translated, **not** renamed per hotel, and **not** stored as a display-label table.

Existing catalogue (unchanged semantics):

| Code | Typical use |
|------|-------------|
| `workforce.read` | Departments/positions; operational workforce |
| `workforce.manage` | Hire / transfer / end; maintain org structure |
| `hr.employee.read` | Personel directory and card (non-restricted) |
| `hr.employee.manage` | Edit/save Personel Master |
| `hr.employee.sensitive.read` | Restricted HR fields |
| `room-operations.read` | Oda Operasyonları |
| `room-operations.manage` | Cleaning work |
| `room-operations.inspect` | Inspection |
| `maintenance.read` | Teknik Servis |
| `maintenance.manage` | Create / assign / classify |
| `maintenance.resolve` | Start / unable / resume / resolve |

AUTH-01 adds administration capabilities:

| Code | Typical use |
|------|-------------|
| `authorization.users.manage` | Create users, memberships, role assignments |
| `authorization.roles.manage` | Create/deactivate roles; change role permissions |

Policies continue to require **permission claims**, never `Role.Name`.

Unknown codes cannot be assigned to a role.

## Roles (database)

| Field | Role |
|-------|------|
| Id | Guid |
| OrganizationId | Role belongs to one organization |
| Name | Customer-visible working name (business data, not translated) |
| Code | Stable per organization (`hr-manager`) |
| ScopeType | `Organization` or `Property` |
| IsSystemTemplate | Seeded default; customer may still deactivate |
| IsActive | Inactive roles contribute no permissions |

Runtime authorization **must not** say `if (role.Name == "HR Manager")`.

A Property-scoped role may only be assigned to a membership with `PropertyId` set. An Organization-scoped role may only be assigned to a membership with `PropertyId` null.

## RolePermission

```text
RolePermission (RoleId, PermissionCode)
```

No duplicated display labels. Many-to-many via this join. Removing a row changes effective access after stamp refresh.

## UserRoleAssignment

```text
UserRoleAssignment (MembershipId, RoleId)
```

Multiple roles per membership. Effective permissions = **union** of permissions from **active** roles on the **active** membership.

Do not copy permission rows onto the user when a role changes. Recompute from the join tables.

## Templates (seed, not semantics)

Seeded for the development/pilot organization. Customers may create “Gece İK Yetkilisi” and tick permissions. Template **display** names may be localized in admin UI; the stored `Name` is data.

| Code | Scope | Bundle |
|------|-------|--------|
| `development-superuser` | Property | All catalogue permissions including authorization.* |
| `hr-manager` | Property | workforce.read/manage, hr.employee.read/manage/sensitive.read |
| `hr-specialist` | Property | Same HR bundle as manager in this slice (no specialist/manager deny split yet) |
| `room-operations-manager` | Property | room-operations.read/manage/inspect |
| `room-attendant` | Property | room-operations.read/manage |
| `room-inspector` | Property | room-operations.read/inspect |
| `maintenance-manager` | Property | maintenance.read/manage/resolve |
| `maintenance-technician` | Property | maintenance.read/resolve |

`hr.specialist` is a **template only** in AUTH-01. No `hr.specialist@localhost` user (unchanged from development personas).

## Delete safety

Do not hard-delete a role that still has assignments. Deactivate (`IsActive = false`). Permission removal is allowed and is auditable.

## Effective calculation

```text
active membership
  → assignments whose role.IsActive
    → RolePermission.PermissionCode
      → distinct union
```

Inputs that grant **nothing** by themselves: email, username, Position, Department, inactive membership, inactive role, Role.Name.

## Claim issuance

`HuGuUserClaimsPrincipalFactory` writes `permission` claims from that union, plus membership context claims. `AspNetUserClaims` is **not** the source of ERP permissions after AUTH-01. Development seed removes previously copied permission claims from persona users.
