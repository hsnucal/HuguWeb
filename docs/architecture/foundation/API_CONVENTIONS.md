# API conventions

> **Status:** Accepted — ARCH-01 (2026-08-25).

## Problem Details

RFC 7807 Problem Details. Machine-readable `code`. Localized `title` / `detail` via domain `.resx`. Field errors: stable field codes. Frontend must not parse TR/EN sentences. HTTP status preserved (401 / 403 / 404 / 409 / 400).

`correlationId` is copied from the ASP.NET request/trace identifier (middleware) into Problem Details and the response header. Logs stay English/structured. Do not add distributed tracing products now.

## List queries

For large ERP lists (not every tiny endpoint):

- `page`, `pageSize` (max **100**, default 50), `sort` (whitelist), `direction`, `search`, filters
- Server owns filtering/sorting. No client SQL field names.
- Response: `items`, `page`, `pageSize`, `totalCount` (`ListQuery` / `PagedResponse` in API `Http`)

Page-based pagination is enough at current CRUD scale. No GraphQL/OData. Cursor only if a domain proves need.

## Versioning

Do **not** introduce `/api/v1` for ceremony. One SPA + one backend, pre-public API.

Introduce URL or header versioning when a second public consumer exists or a breaking public contract ships.
