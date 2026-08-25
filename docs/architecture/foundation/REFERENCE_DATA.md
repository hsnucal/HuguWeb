# Reference data

> **Status:** Accepted — ARCH-01 (2026-08-25).

Do **not** create one universal `ReferenceData` / `GovernmentCode` table.

| Category | Examples | Storage |
|----------|----------|---------|
| A. Domain enum / closed state | `RoomReadiness`, issue status | Code enum |
| B. Customer-maintained | `MaintenanceIssueCategory`, departments | DB, property/org scoped |
| C. External / versioned catalogue | SGK occupation, universities, country codes | Explicit catalogue type: `Code`, name, `IsActive`, `Source`, `SourceVersion` when meaningful |

Rules for C: stable code is identity; display name is metadata; referenced rows are deactivated, not physically deleted; updates are idempotent.
