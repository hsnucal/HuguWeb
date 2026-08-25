# Lifecycle / deletion

> **Status:** Accepted — ARCH-01 (2026-08-25).

No universal `IsDeleted`. No generic soft-delete EF filter.

| Concept | Lifecycle |
|---------|-----------|
| Employee | Retained; employment ends |
| Position / Department | Deactivate per existing rules |
| SGK lookup | Deactivate |
| MaintenanceIssue | Terminal resolved |
| Room | Active / inactive if accepted |
| Membership | `IsActive`; deactivation grants nothing |

Hard delete is not the default for operational records that other modules may have referenced by Guid.
