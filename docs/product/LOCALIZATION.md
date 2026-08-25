# Localization

HuGuWeb UI language is a user preference, not a hotel or tenant setting.

## Supported languages

| Code | Display name |
|------|----------------|
| `tr` | Türkçe |
| `en` | English |
| `ru` | Русский |

Codes are stable language identifiers. Display names are native autonyms. Country locales such as `tr-TR` / `en-US` / `ru-RU` are not stored as the preference value.

The brand name **HuGuWeb** is not translated.

## Default and fallback

Application default: **Turkish (`tr`)** — Turkey-first product direction.

Unsupported browser locales are ignored. The UI never silently activates an unknown language code.

Unauthenticated precedence:

1. Previously selected browser language (`localStorage` key `huguweb.preferredLanguage`)
2. First supported language from the browser (`navigator.languages`, matched by primary subtag only)
3. Application default (`tr`)

Authenticated precedence:

1. Persisted user language on the Identity user (source of truth)
2. Browser preference
3. Application default

## Persistence

| State | Where the language lives |
|-------|--------------------------|
| Before sign-in | Browser `localStorage` only (language code, never credentials) |
| After sign-in | `ApplicationUser.PreferredLanguage` in PostgreSQL |
| After logout | Browser preference is kept so login stays in the last used language |

If the user changes language on the login screen, that choice is written to the authenticated user after a successful sign-in when it differs from the saved value.

If the user has no saved preference yet, the current UI language is saved on first authenticated session.

Later visits restore the backend value after authentication, including on another browser or device.

## API

Authenticated session payloads include `preferredLanguage`.

Update endpoint:

- `PATCH /api/auth/preferences/language`
- Body: `{ "language": "tr" | "en" | "ru" }`
- Requires the existing cookie session and CSRF header
- Invalid values return Problem Details (`400`)
- Narrow request model: language only

## Translation keys

Frontend resources are composed from domain modules under `src/frontend/web/src/i18n/`:

- `common/` — `common.*`, `navigation.*`, `operations.*`
- `auth/`
- `workforce/`
- `hr/` — existing `personnel.*` keys
- `room-operations/`
- `technical-service/` — existing `maintenance.*` keys
- `authorization/`

Runtime still uses one i18next `translation` namespace. See [LOCALIZATION_ARCHITECTURE](../security/authorization/LOCALIZATION_ARCHITECTURE.md).

API Problem Details are localized with `.resx` + `IStringLocalizer` in the host, keyed by stable `code`. Backend does not use TypeScript locale files.

## Locale vs currency

UI language and regional formatting are related but not identical. Date and number formatting currently follow the active UI language via `Intl`. Currency rules are not defined yet and must not be inferred from language alone (a Turkish-language user may still work in another currency later).

## Library

The SPA uses `i18next` and `react-i18next` for React bindings, interpolation, and a small fallback path. A custom i18n framework was not added.
