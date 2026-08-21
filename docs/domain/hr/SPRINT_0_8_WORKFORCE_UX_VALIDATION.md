# Sprint 0.8 — Workforce UX Validation + Local Development Experience

> Implementation note. Does not redefine Accepted architecture or Workforce domain documents.

Sprint 0.7B remains the domain baseline. This sprint adds a Windows-first local launcher and validates the Personel product UX against the already implemented Organization & Workforce model.

## Problems found in 0.7B UX

The 0.7B Personel screens were a working first slice, but they still read like a technical demonstration:

- Active, scheduled, and former people were stacked on one page. Scheduled/former sections disappeared when empty, so an HR user could not answer “who is starting soon?” or “who left?” at a glance.
- The directory led with sicil no rather than name, and omitted start date.
- There was no search and no department filter.
- Hire was a flat six-field form using employment/assignment mental model rather than hotel HR language.
- Transfer did not show current vs new department/position, and defaulted the effective date to today even when that date is invalid for an assignment that started today.
- End employment said history remains, but did not clearly say the record is not deleted and the person leaves the active list.
- Employee detail mixed identity, current work, and history without a clear hierarchy. History hid the current row, so a first hire looked like “no history”.
- Mutation actions were always visible. Session payloads did not include permission claims, so a read-only user would see hire/transfer/end/create actions that then fail.
- Loading states were missing or empty.

## Changes made

### Local development

- Added `dev.ps1` as the primary Windows startup path from the repository root.
- Added `dev-stop.ps1` to stop only launcher-started API/frontend process trees.
- Updated `docs/engineering/LOCAL_DEVELOPMENT.md` and the root README.

### Personel UX

- One Personel destination with subnav: Personel / Departmanlar / Pozisyonlar.
- Directory uses segments for Aktif personel, Planlanan işe girişler, and İşten ayrılanlar, with counts.
- Directory columns: Ad soyad, Sicil no, Departman, Pozisyon, İşe giriş tarihi, Durum.
- Name / sicil search and department filter.
- Hire is **Yeni personel**, grouped as kişisel bilgiler / işe giriş / departman ve pozisyon. Department and Position remain independent selectors.
- Transfer is **Görev değişikliği**, with current vs new and a product explanation. Effective date defaults to the earliest valid date (the day after the current duty started, or today if later). The date field uses that minimum. D-1/D language is not shown.
- End employment has one confirmation: the record is not deleted, history is kept, and the person leaves the active list.
- Employee detail shows identity, current/last work, and a dated work-history timeline. GUIDs are not shown as labels.
- Departments/Positions stay simple create/rename/activate/deactivate lists, with Aktif/Pasif, no physical delete, and Position still explained as property-scoped.
- Loading, empty, and error states; mutation actions require `workforce.manage`.
- Session/login/me now return existing permission claims so the UI can hide misleading actions. Authorization policies were not redesigned.

## Decisions retained

- Modular monolith, Clean Architecture, ASP.NET Core / .NET 10, React 19 + Vite 8, PostgreSQL 18, EF Core 10, REST, Identity cookie auth, permission policies, TR/EN/RU.
- Personel stays one sidebar destination. Segments were chosen over five sidebar items.
- No component library, no table package, no Docker Compose, no npm orchestrator.
- Permission-cookie renewal limitation remains accepted technical debt. The UI reports the claims present in this session (cookie) or, on login, claims loaded for the new cookie.

## Domain assumptions NOT changed

- Organization → Property → Department and Position (Position is Property-scoped, not Department-owned).
- Employee → Employment → Assignment independently references Department and Position.
- Employee ≠ ApplicationUser.
- Position/Department do not grant permissions.
- Personnel number is unique within Organization, including former staff.
- History is preserved; there is no physical delete of employees, employments, or assignments.
- Transfer still closes the previous primary on D-1 and starts the new primary on D. Same-day transfer on the assignment start date remains invalid. This sprint did not change that invariant.

## Visual observations

Reviewed in Chrome at ~1440, ~1024, and ~768.

- Personel is a directory, not a KPI wall. Purple is reserved for the current nav item, segment, and primary hire/transfer actions.
- Surfaces stay warm (`--color-surface-page` / elevated cards). Contrast on name, sicil, and status badges is comfortable.
- At ~1440 the six-column directory is scannable. Below ~960 the start-date column hides; at ~900 rows stack with labels so tablet-ish widths remain usable without becoming a phone app.
- Destructive **İşten ayrılma** stays outline/danger, distinct from **Görev değişikliği**.
- After transfer, current work shows the new department/position and the timeline keeps the previous period.
- After end employment, **İşten ayrılanlar** still lists the person; detail shows last work and the full timeline.
- Russian strings (`Запланированный выход`, `Табельный номер`, `К списку персонала`) wrap in the existing subnav/tabs without overflowing the shell.
- Customer-defined department/position names remain data and are not auto-translated.

## Remaining UX risks

- Same-day “hired into the wrong department, correct immediately” is blocked by the accepted D-1 transfer rule. The form now defaults to the next valid date and explains the restriction in product language. Whether same-day correction should be allowed is a future domain question, not a silent change.
- There is still no dedicated read-only development user. Mutation hiding was verified by code and by the managing development user seeing actions; a true `workforce.read`-only account was not available in this environment.
- Session permission claims follow the cookie. Permission changes still require a new sign-in.
- Local verification created extra development departments/employees. They are data, not schema.

## Future HR questions discovered

- Should an HR user be able to correct department/position on the same calendar day as işe giriş?
- Is “Görev değişikliği” the preferred term versus “Nakil” / “Transfer” in independent mid-size hotels?
- Should former staff remain one tab on Personel, or later become a separate archive with rehire?
- When should scheduled starts (future işe giriş) be the default hire date vs today?

## Government / later domains

Not implemented: SGK, Police KBS, Jandarma KBS, Housekeeping, Leave, Attendance, Payroll, or any new business domain.
