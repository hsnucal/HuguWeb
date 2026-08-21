# Local Development

This document covers the Sprint 0.3B application foundation. It does not repeat architecture ADRs.

## Prerequisites

- .NET 10 SDK
- Node.js 24 LTS and npm
- PostgreSQL 18 for identity persistence, workforce persistence, and authentication runtime checks

PostgreSQL is required for login, session cookies, and `/health/ready`. The API can start without it; `/health` still reports process liveness.

Local PostgreSQL 18 (this machine):

- Binaries: `C:\Program Files\PostgreSQL\18`
- Data directory: `C:\Program Files\PostgreSQL\18\data`
- Database: `huguweb_dev`
- Application role: `huguweb` (not a superuser)
- Connection string and passwords: .NET User Secrets / local environment only

The Windows installer cluster init failed on this host because the OS locale name `Turkish_Türkiye.1254` contains non-ASCII characters. The cluster was initialized with `locale=C` / UTF8. A Windows service was not registered (service manager elevation). Start/stop as the installing user:

```bash
"C:\Program Files\PostgreSQL\18\bin\pg_ctl.exe" start -D "C:\Program Files\PostgreSQL\18\data"
"C:\Program Files\PostgreSQL\18\bin\pg_ctl.exe" stop -D "C:\Program Files\PostgreSQL\18\data"
```

Also set the connection string locally (never commit the password):

```bash
dotnet user-secrets set "ConnectionStrings:IdentityDatabase" "Host=localhost;Port=5432;Database=huguweb_dev;Username=huguweb;Password=<local-app-password>" --project src/backend/HuGuWeb.Api
```

## Repository layout

- `src/backend/HuGuWeb.Api` — ASP.NET Core API host
- `src/backend/modules/HuGuWeb.Workforce` — Organization & Workforce domain and use cases
- `src/backend/modules/HuGuWeb.Workforce.Infrastructure` — Workforce EF Core mapping
- `src/frontend/web` — React 19 + Vite 8 SPA
- `tests/HuGuWeb.UnitTests`
- `tests/HuGuWeb.ArchitectureTests`

## Configuration and secrets

Do not commit real passwords.

Connection string:

- Config key: `ConnectionStrings:IdentityDatabase`
- Environment override: `ConnectionStrings__IdentityDatabase`

`appsettings.Development.json` contains a clearly fake local placeholder password. Override it for your machine.

Development user (Development environment only):

```bash
dotnet user-secrets set "DevelopmentUser:Email" "dev@localhost" --project src/backend/HuGuWeb.Api
dotnet user-secrets set "DevelopmentUser:Password" "<choose-a-local-password-meeting-Identity-rules>" --project src/backend/HuGuWeb.Api
```

Identity password rules at bootstrap: at least 12 characters, with upper, lower, digit, and non-alphanumeric characters.

If those values are missing, the API skips seeding and logs that fact. There is no public registration endpoint.

## Database

Create the identity schema with EF Core migrations. Do not use `EnsureCreated()`. Do not apply migrations automatically in Production. Development startup also does not auto-apply migrations.

```bash
dotnet ef database update --project src/backend/HuGuWeb.Api --context AppIdentityDbContext
dotnet ef database update --project src/backend/modules/HuGuWeb.Workforce.Infrastructure --startup-project src/backend/HuGuWeb.Api --context WorkforceDbContext
```

The Identity migrations contain ASP.NET Core Identity tables and nullable `PreferredLanguage` on `AspNetUsers` (`tr` / `en` / `ru`). The Workforce migration adds Organization & Workforce tables only. Apply pending migrations with the commands above; do not auto-apply in Production.

## Backend

```bash
dotnet restore
dotnet run --project src/backend/HuGuWeb.Api --launch-profile http
```

Useful URLs:

- Liveness: `http://localhost:5116/health`
- Readiness (database): `http://localhost:5116/health/ready`
- OpenAPI (Development only): `http://localhost:5116/openapi/v1.json`
- HTTPS profile is available as `https` (`https://localhost:7138`) for direct API use

## Frontend

```bash
cd src/frontend/web
npm install
npm run dev
```

The Vite dev server proxies `/api`, `/health`, and `/openapi` to `http://localhost:5116`. Leave `VITE_API_BASE_URL` empty for that same-origin proxy. Copy `.env.example` only if you need a local override; do not commit `.env`.

## Tests

```bash
dotnet test
cd src/frontend/web
npm run lint
npm run build
```

Frontend automated tests are deferred until there is product UX behavior to protect. Current screens only prove sign-in routing.

PostgreSQL integration tests are deferred until a local or CI database is authorized. Do not treat in-memory or SQLite substitutes as identity persistence verification.

## Authentication notes

Web authentication uses an HTTP-only cookie issued by ASP.NET Core Identity. Access tokens are not stored in `localStorage`.

### Cookie strategy

| Setting | Development | Production |
| --- | --- | --- |
| Auth cookie name | `HuGuWeb.Auth` | `__Host-HuGuWeb.Auth` |
| HttpOnly | yes | yes |
| Secure | `SameAsRequest` | always |
| SameSite | Lax | Lax |
| Path | `/` | `/` |

Development uses HTTP between Vite (`http://localhost:5173`) and the API (`http://localhost:5116`) so Secure cookies are not required for local proxying. That is a local-only trade-off. Production must use HTTPS; the `__Host-` cookie prefix then requires Secure, Path `/`, and no Domain attribute. Serve the SPA and API same-site, preferably same-origin behind a reverse proxy.

Do not set `SameSite=None` to make mixed HTTP/HTTPS local origins work.

### CORS

Development origins are listed in `appsettings.Development.json`. Production `Cors:AllowedOrigins` is empty by default and denies browser origins until configured. `AllowAnyOrigin` is never combined with credentials.

### CSRF

SameSite=Lax is not enough if the SPA and API are later hosted as sibling HTTPS subdomains (same-site, cross-origin). Mutating auth endpoints therefore require ASP.NET Core antiforgery:

1. `GET /api/auth/csrf` stores the antiforgery cookie (HttpOnly) and returns the request token
2. The SPA keeps that token in memory and sends `X-XSRF-TOKEN` on POST
3. Login and logout validate the token

No custom cryptography is used.

## Observability

Logs are JSON on the console, with a correlation/request id. Incoming `X-Correlation-ID` is used only when it is a short safe token; otherwise the current trace/request id is used. OpenTelemetry ASP.NET Core tracing/metrics are registered without an external exporter.
