# Scaling (100 properties)

> **Status:** Accepted — ARCH-01 (2026-08-25).

No need to seed 100 hotels. The architecture does **not** require 100 tables, 100 DbContexts, 100 role classes, or 100 menu definitions.

| Concern | Model |
|---------|--------|
| Properties | Rows in `Properties` |
| Employees (~30k) | Rows in `Employees` with `OrganizationId`; assignments via departments’ `PropertyId` |
| Roles | Rows per organization; `UNIQUE(OrganizationId, Code)` |
| Menus | Derived from effective permission codes |
| Queries | Filter by tenant context + existing indexes on `OrganizationId` / `PropertyId` / membership keys |

Expected path: one PostgreSQL, shared tables, indexes on tenant keys. Dedicated database/shard for a huge customer is a later operations choice, not a domain-model fork.
