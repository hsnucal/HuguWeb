# Time and timezone

> **Status:** Accepted — ARCH-01 (2026-08-25).

## TimeProvider

Production uses `TimeProvider.System` (DI). Domain clocks (`IWorkforceClock`, `IRoomOperationsClock`, `ITechnicalServiceClock`) wrap it. Tests use fakes (`FakeClock`, `FakeTimeProvider` where needed). Do not add a parallel `IClock`.

Do not use `DateTime.Now` for business or audit timestamps. Seeders may use `DateTime.UtcNow` for one-off Development data.

## Timestamp kinds

| Kind | Type | Examples |
|------|------|----------|
| Technical | UTC `DateTimeOffset` (`*Utc` / `OccurredAtUtc`) | Created, audit, history |
| Business date | `DateOnly` | Employment start, leave |
| Property-local operational time | Interpret UTC via `Property.TimeZoneId` | Future “today at this hotel” |

## Property timezone

`Property.TimeZoneId` is required, max 64 chars, validated with `TimeZoneInfo.TryFindSystemTimeZoneById` (IANA on this stack; Windows may accept IANA on .NET 10).

Do **not** spread `Europe/Istanbul` through domain or API code. Development seed/config may set that id explicitly (`DevelopmentWorkforceSeeder.DevelopmentTimeZoneId`, `Workforce:TimeZoneId` in `appsettings.Development.json`).
