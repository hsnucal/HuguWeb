# Localization architecture

> **Status:** Accepted — AUTH-01 (2026-08-25).

## Frontend vs backend (do not mix)

| Surface | Language | Resource type | Why |
|---------|----------|---------------|-----|
| React SPA | TypeScript | Domain `.ts` locale modules composed at runtime | UI chrome, labels, empty states, known `code` → message maps |
| ASP.NET API | C# | `.resx` + `IStringLocalizer` | Problem Details title/detail for the request culture |
| Domain / Application | C# | **Stable error codes** + English technical strings for logs | Domain must not depend on `IStringLocalizer` |

**Backend localization does not use TypeScript files.** A `.ts` file cannot be the resource for `IStringLocalizer`. Putting UI copy in `HuGuWeb.Workforce` via resx would also pull presentation into the application layer; AUTH-01 keeps resx in the **API host**.

## Frontend layout

```text
src/frontend/web/src/i18n/
  types.ts                 composed Translations
  i18n.ts                  merge domains per language
  common/{tr,en,ru}.ts     common, navigation, operations
  auth/{tr,en,ru}.ts
  workforce/{tr,en,ru}.ts
  hr/{tr,en,ru}.ts         existing `personnel.*` keys (no blind rename)
  room-operations/{tr,en,ru}.ts
  technical-service/{tr,en,ru}.ts
  authorization/{tr,en,ru}.ts
```

Runtime still has **one** i18next resource (`translation` namespace). Keys stay stable (`personnel.save`, `maintenance.empty`) so TSX does not churn.

Languages: `tr`, `en`, `ru`. No regression. Domain objects are typed so missing keys fail the TypeScript build.

Technical constants (permission codes, enum values, ISO country codes, university lists) stay out of locale files.

## Backend layout

```text
HuGuWeb.Api/Localization/
  CommonMessages[.tr|.en|.ru].resx
  AuthMessages[.tr|.en|.ru].resx
  AuthorizationMessages[.tr|.en|.ru].resx
  HrMessages[.tr|.en|.ru].resx
  WorkforceMessages[.tr|.en|.ru].resx
  RoomOperationsMessages[.tr|.en|.ru].resx
  TechnicalServiceMessages[.tr|.en|.ru].resx
```

Neutral `.resx` files are English fallbacks. Domain marker types live beside the resources. `ApiErrorLocalizer` composes them. There is no giant `ApiMessages` resource.

Request culture: authenticated cookie `PreferredLanguage` when present, else `Accept-Language`, else `tr`.

`CustomizeProblemDetails` maps `extensions.code` → `error.{code}.title` / `error.{code}.detail` when the resource exists. Application `WorkforceError.Title` remains English for logs and as fallback.

Do not localize log messages.

## Error codes and Problem Details

Application failures keep machine-readable codes (`sgk-workplace-inactive`, `employee-not-found`).

Conceptual response:

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "localized title",
  "detail": "localized detail",
  "code": "sgk-workplace-inactive",
  "errors": {
    "sgkWorkplaceRegistrationId": ["sgk-workplace-inactive"]
  },
  "correlationId": "..."
}
```

Field `errors` values remain **codes**, not prose, so the SPA does not parse Turkish text.

## One consistent frontend strategy

Avoid three competing sources.

| Kind | Source of truth |
|------|-----------------|
| Static UI | Frontend domain locales |
| API failure the SPA already knows | `code` → domain locale key (existing `workforceErrorKey` pattern) |
| Unknown `code` | Backend localized `title` / `detail` |
| Never | Parse `detail` as a language to infer meaning |

Permission **codes** are not translated. Admin checkboxes use authorization locale labels (Personel Yönetimi / Personnel Management / Управление персоналом).

## Application default

Unchanged: UI language is a **user** preference, not a hotel setting ([product/LOCALIZATION.md](../../product/LOCALIZATION.md)).
