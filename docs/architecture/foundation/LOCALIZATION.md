# Localization

> **Status:** Accepted — ARCH-01 (2026-08-25).

## Backend

Domain-owned `.resx` in the API presentation layer (Clean Architecture: Domain stays unaware of `IStringLocalizer`):

- `CommonMessages`
- `AuthMessages`
- `AuthorizationMessages`
- `HrMessages`
- `WorkforceMessages`
- `RoomOperationsMessages`
- `TechnicalServiceMessages`

`ApiErrorLocalizer` composes them by error code (`error.{code}.title` / `.detail`). Logs are not translated.

## Frontend

Domain TypeScript modules composed at runtime: `common`, `auth`, `authorization`, `workforce`, `hr`, `room-operations`, `technical-service`. No giant global locale file. No duplicate key ownership.
